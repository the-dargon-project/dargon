using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dargon.Management.Server;
using Dargon.PortableObjects;
using Dargon.Services;
using NLog;

namespace Dargon.Ryu {
   public class RyuContainerImpl : RyuContainer {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      private readonly PofContext pofContext;
      private readonly PofSerializer pofSerializer;

      private readonly Dictionary<Type, RyuPackageV1TypeInfo> typeInfosByType = new Dictionary<Type, RyuPackageV1TypeInfo>();
      private readonly ConcurrentDictionary<Type, object> instancesByType = new ConcurrentDictionary<Type, object>();
      private readonly ISet<Type> remoteServices = new HashSet<Type>();

      private readonly ISet<Assembly> loadedAssemblies = new HashSet<Assembly>();
      private readonly ISet<RyuPackageV1> packages = new HashSet<RyuPackageV1>();

      public RyuContainerImpl(PofContext pofContext, PofSerializer pofSerializer) {
         this.pofContext = pofContext;
         this.pofSerializer = pofSerializer;
      }

      public void Setup() {
         instancesByType[typeof(IPofContext)] = pofContext;
         instancesByType[typeof(PofContext)] = pofContext;
         instancesByType[typeof(IPofSerializer)] = pofSerializer;
         instancesByType[typeof(PofSerializer)] = pofSerializer;

         var stack = new Stack<Assembly>();
         stack.Push(Assembly.GetCallingAssembly());
         stack.Push(Assembly.GetEntryAssembly());
         HashSet<string> loadedAssemblyFullNames = new HashSet<string>();
         while (stack.Any()) {
            var assembly = stack.Pop();
            if (assembly == null) {
               continue;
            }
            var referencedAssemblyNames = new HashSet<AssemblyName>(assembly.GetReferencedAssemblies());
            foreach (var referencedAssemblyName in referencedAssemblyNames) {
               if (!loadedAssemblyFullNames.Contains(referencedAssemblyName.FullName)) {
                  var loadedAssembly = Assembly.Load(referencedAssemblyName);
                  stack.Push(loadedAssembly);
                  loadedAssemblyFullNames.Add(referencedAssemblyName.FullName);
               }
            }
         }
         LoadAdditionalAssemblies();
      }

      public bool LoadAdditionalAssemblies() {
         var assemblies = new HashSet<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
         assemblies.ExceptWith(loadedAssemblies);
         loadedAssemblies.UnionWith(assemblies);

         if (!assemblies.Any()) {
            return false;
         }

         var packageTypes = assemblies.AsParallel().SelectMany(assembly => assembly.GetTypes().Where(x => typeof(RyuPackageV1).IsAssignableFrom(x))).ToList();
         var packageInstances = packageTypes.Select(Activator.CreateInstance).Cast<RyuPackageV1>().ToList();
         foreach (var package in packageInstances) {
            logger.Info("Found package: " + package);
            this.packages.Add(package);
            foreach (var typeInfo in package.TypeInfoByType.Values) {
               if (typeInfo.Flags.HasFlag(RyuTypeFlags.IgnoreDuplicates) &&
                   typeInfosByType.ContainsKey(typeInfo.Type)) {
                  // Do nothing!
               } else {
                  typeInfosByType.Add(typeInfo.Type, typeInfo);
               }
            }
            foreach (var remoteServiceType in package.RemoteServiceTypes) {
               remoteServices.Add(remoteServiceType);
            }
            foreach (var typeInfo in package.TypeInfoByType.Values) {
               if (typeInfo.Flags.HasFlag(RyuTypeFlags.PofContext)) {
                  this.pofContext.MergeContext((PofContext)typeInfo.GetInstance(this));
               }
            }
         }

         LoadAdditionalAssemblies();

         foreach (var package in packageInstances) {
            foreach (var typeInfo in package.TypeInfoByType.Values) {
               if (typeInfo.Flags.HasFlag(RyuTypeFlags.Required)) {
                  Get(typeInfo.Type);
               }
            }

            IServiceClient serviceClient = null;
            foreach (var typeInfo in package.TypeInfoByType.Values) {
               if (typeInfo.Flags.HasFlag(RyuTypeFlags.Service)) {
                  serviceClient = serviceClient ?? Get<IServiceClient>();
                  serviceClient.RegisterService(typeInfo.GetInstance(this), typeInfo.Type);
               }
            }

            ILocalManagementServer localManagementServer = null;
            foreach (var typeInfo in package.TypeInfoByType.Values) {
               if (typeInfo.Flags.HasFlag(RyuTypeFlags.ManagementObject)) {
                  localManagementServer = localManagementServer ?? Get<ILocalManagementServer>();
                  localManagementServer.RegisterInstance(typeInfo.GetInstance(this));
               }
            }
         }

         return true;
      }

      public T Get<T>() {
         return (T)Get(typeof(T));
      }

      public object Get(Type type) {
         try {
            object result;
            if (instancesByType.TryGetValue(type, out result)) {
               return result;
            } else {
               return GetUninstantiated(type);
            }
         } catch (Exception e) {
            throw new AggregateException("While constructing " + type.FullName, e);
         }
      }

      public void Set<T>(T instance) {
         Set(typeof(T), instance);
      }

      public void Set(Type type, object instance) {
         instancesByType.AddOrUpdate(
            type,
            add => instance,
            (update, existing) => instance
         );
      }

      public T Construct<T>() {
         return (T)Construct(typeof(T));
      }

      /// <summary>
      /// Constructs a new instance of the given type, even if an instance of
      /// the given type already exists in the container.
      /// container.
      /// </summary>
      public object Construct(Type type) {
         RyuPackageV1TypeInfo typeInfo;
         if (!typeInfosByType.TryGetValue(type, out typeInfo)) {
            throw new InvalidOperationException($"Instance would default to singleton as type {type.FullName} was not registered as instance.");
         } else if (typeInfo.Flags.HasFlag(RyuTypeFlags.Cache)) {
            throw new InvalidOperationException($"Attempting to construct unique instance of {type.FullName} but typeInfo has cache flag.");
         } else {
            return GetUninstantiated(type);
         }
      }

      public T ForceConstruct<T>() {
         return (T)ForceConstruct(typeof(T));
      }

      public object ForceConstruct(Type type) {
         return ConstructAndInitialize(GetRyuConstructorOrThrow(type));
      }

      public void Touch<T>() { Touch(typeof(T)); }
      public void Touch(Type type) { LoadAdditionalAssemblies(); }

      private object GetUninstantiated(Type type) {
         RyuPackageV1TypeInfo typeInfo;
         if (typeInfosByType.TryGetValue(type, out typeInfo)) {
            if (typeInfo.Flags.HasFlag(RyuTypeFlags.Cache)) {
               return instancesByType.GetOrAdd(type, add => typeInfo.GetInstance(this));
            } else {
               return typeInfo.GetInstance(this);
            }
         } else if (type.IsInterface) {
            if (remoteServices.Contains(type)) {
               var serviceClient = Get<IServiceClient>();
               return typeof(IServiceClient).GetMethod(nameof(serviceClient.GetService)).MakeGenericMethod(type).Invoke(serviceClient, null);
            } else if (!LoadAdditionalAssemblies()) {
               throw new ImplementationNotDefinedException(type);
            } else {
               return GetUninstantiated(type);
            }
         } else {
            return ConstructAndInitialize(GetRyuConstructorOrThrow(type));
         }
      }

      private object ConstructAndInitialize(ConstructorInfo constructor) {
         var parameters = constructor.GetParameters();
         var parameterValues = new object[parameters.Length];
         for (var i = 0; i < parameters.Length; i++) {
            parameterValues[i] = Get(parameters[i].ParameterType);
         }
         var result = constructor.Invoke(parameterValues);
         var initializeMethod = result.GetType().GetMethods().FirstOrDefault(m => m.Name.Equals("Initialize", StringComparison.OrdinalIgnoreCase));
         initializeMethod?.Invoke(result, null);
         return result;
      }

      private ConstructorInfo GetRyuConstructorOrThrow(Type type) {
         var constructors = type.GetConstructors();
         if (constructors.Length == 0) {
            throw new NoConstructorsFoundException(type);
         } else if (constructors.Length == 1) {
            return constructors.First();
         } else {
            var ryuConstructors = constructors.Where(FilterRyuConstructor).ToList();
            if (ryuConstructors.Count != 1) {
               throw new MultipleConstructorsFoundException(type);
            } else {
               return ryuConstructors.First();
            }
         }
      }

      private bool FilterRyuConstructor(ConstructorInfo constructor) {
         try {
            constructor.GetCustomAttribute<RyuConstructorAttribute>();
            return true;
         } catch (Exception e) {
            return false;
         }
      }

      public void DebugEnumerateInstances() {
         foreach (var kvp in this.instancesByType) {
            Console.WriteLine(kvp.Value);
         }
      }
   }
}