using Dargon.Management.Server;
using Dargon.PortableObjects;
using Dargon.Services;
using ItzWarty.Collections;
using NLog;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SCG = System.Collections.Generic;

namespace Dargon.Ryu {
   public class RyuContainerImpl : RyuContainer {
      private readonly IPofContext pofContext;
      private readonly IPofSerializer pofSerializer;

      private readonly SCG.IDictionary<Type, RyuPackageV1TypeInfo> typeInfosByType;
      private readonly IConcurrentDictionary<Type, object> instancesByType;
      private readonly SCG.ISet<Type> remoteServices;

      private readonly SCG.ISet<Assembly> loadedAssemblies = new SCG.HashSet<Assembly>();
      private readonly SCG.ISet<Type> loadedPackageTypes = new SCG.HashSet<Type>();

      private Logger logger = LogManager.CreateNullLogger();

      public RyuContainerImpl(IPofContext pofContext, IPofSerializer pofSerializer, SCG.IDictionary<Type, RyuPackageV1TypeInfo> typeInfosByType, IConcurrentDictionary<Type, object> instancesByType, SCG.ISet<Type> remoteServices) {
         this.pofContext = pofContext;
         this.pofSerializer = pofSerializer;
         this.typeInfosByType = typeInfosByType;
         this.instancesByType = instancesByType;
         this.remoteServices = remoteServices;
      }

      internal void Initialize() {
         instancesByType.TryAdd(typeof(IPofContext), pofContext);
         instancesByType.TryAdd(typeof(IPofSerializer), pofSerializer);
         instancesByType.TryAdd(typeof(RyuContainer), this);
         instancesByType.TryAdd(typeof(RyuContainerImpl), this);
      }

      public void SetLoggerEnabled(bool isLoggerEnabled) {
         if (isLoggerEnabled) {
            logger = LogManager.GetCurrentClassLogger();
         } else {
            logger = LogManager.CreateNullLogger();
         }
      }

      public void Setup() {
         logger.Info("Touching entry assembly...");
         TouchAssembly(Assembly.GetEntryAssembly());

         logger.Info("Touching calling assembly...");
         TouchAssembly(Assembly.GetCallingAssembly());
      }

      private HashSet<Assembly> GetDirectlyReferencedAssemblies(Assembly seedAssembly) {
         var assemblyNames = seedAssembly.GetReferencedAssemblies().Where(x => x != null).ToList();
         var assemblies = new HashSet<Assembly>();
         foreach (var assemblyName in assemblyNames) {
            logger.Info("Loading assembly: " + assemblyName);
            var assembly = Assembly.Load(assemblyName);
            Trace.Assert(assembly != null, "assembly != null");
            assemblies.Add(assembly);
         }
         return assemblies;
      }

      public bool TouchAssembly(Assembly assembly) {
         bool anyAssembliesLoaded;
         if (assembly == null || loadedAssemblies.Contains(assembly)) {
            anyAssembliesLoaded = false;
         } else {

            logger.Info("Touching assembly: " + assembly.FullName);

            loadedAssemblies.Add(assembly);

            var packageTypes = assembly.GetTypes().Where(x => typeof(RyuPackageV1).IsAssignableFrom(x)).ToList();
            var packageInstances = packageTypes.Select(Activator.CreateInstance).Cast<RyuPackageV1>().ToList();

            TouchPackages(packageInstances, assembly);

            anyAssembliesLoaded = true;
         }
         return anyAssembliesLoaded;
      }

      public void TouchPackages(SCG.IReadOnlyCollection<RyuPackageV1> packageInstancesInput, Assembly seedAssembly = null) {
         var packagesToLoad = new HashSet<RyuPackageV1>();
         foreach (var package in packageInstancesInput) {
            var packageType = package.GetType();
            if (!loadedPackageTypes.Contains(packageType)) {
               packagesToLoad.Add(package);
               loadedPackageTypes.Add(packageType);
            }
         }

         foreach (var package in packagesToLoad) {
            logger.Info("Found package: " + package);
            foreach (var typeInfo in package.TypeInfoByType.Values) {
               if (typeInfo.Flags.HasFlag(RyuTypeFlags.IgnoreDuplicates) &&
                   typeInfosByType.ContainsKey(typeInfo.Type)) {
                  // Do nothing!
               } else {
                  try {
                     typeInfosByType.Add(typeInfo.Type, typeInfo);
                  } catch (ArgumentException) {
                     Trace.WriteLine("While loading typeinfo for type " + typeInfo.Type.FullName);
                     throw;
                  }
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

         if (seedAssembly != null) {
            foreach (var referencedAssembly in GetDirectlyReferencedAssemblies(seedAssembly)) {
               TouchAssembly(referencedAssembly);
            }
         }

         foreach (var package in packagesToLoad) {
            foreach (var typeInfo in package.TypeInfoByType.Values) {
               if (typeInfo.Flags.HasFlag(RyuTypeFlags.Required)) {
                  Get(typeInfo.Type);
               }
            }

            ServiceClient serviceClient = null;
            foreach (var typeInfo in package.TypeInfoByType.Values) {
               if (typeInfo.Flags.HasFlag(RyuTypeFlags.Service)) {
                  serviceClient = serviceClient ?? Get<ServiceClient>();
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
      }


      public void Set<T>(T value) {
         Set(typeof(T), value);
      }

      public void Set(Type type, object value) {
         RyuPackageV1TypeInfo typeInfo;
         if (typeInfosByType.TryGetValue(type, out typeInfo) &&
             !typeInfo.Flags.HasFlag(RyuTypeFlags.Cache)) {
            throw new InvalidOperationException("Cannot set cached value for non-cached type");
         } else {
            instancesByType[type] = value;
         }
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
            Trace.WriteLine("Threw while constructing " + type.FullName);
            throw new RyuGetException(type, e);
         }
      }

      public SCG.IEnumerable<T> Find<T>() {
         var queryType = typeof(T);
         return Find(queryType).Cast<T>();
      }

      public SCG.IEnumerable<object> Find(Type queryType) {
         return instancesByType.Where(kvp => {
            var t = kvp.Value.GetType();
            return !t.IsAbstract && queryType.IsAssignableFrom(t);
         }).Select(kvp => kvp.Value).Distinct();
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
      public void Touch(Type type) { Touch(type.Assembly); }
      public void Touch(Assembly assembly) { TouchAssembly(assembly); }

      private object GetUninstantiated(Type type) {
         RyuPackageV1TypeInfo typeInfo;
         if (typeInfosByType.TryGetValue(type, out typeInfo)) {
            if (typeInfo.Flags.HasFlag(RyuTypeFlags.Cache)) {
               return instancesByType.GetOrAdd(type, new Func<Type, object>(add => typeInfo.GetInstance(this)));
            } else {
               return typeInfo.GetInstance(this);
            }
         } else if (type.IsInterface) {
            if (remoteServices.Contains(type)) {
               var serviceClient = Get<ServiceClient>();
               return typeof(ServiceClient).GetMethod(nameof(serviceClient.GetService)).MakeGenericMethod(type).Invoke(serviceClient, null);
            } else if (!TouchAssembly(type.Assembly)) {
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
            var ryuConstructorAttribute = constructor.GetCustomAttribute<RyuConstructorAttribute>();
            return ryuConstructorAttribute != null;
         } catch (Exception) {
            return false;
         }
      }
   }
}