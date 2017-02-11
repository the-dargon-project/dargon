using Dargon.Vox.Internals;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Dargon.Vox.InternalTestUtils {
   public static class TestTypeRegistrationHelpers {
      private static readonly ConcurrentDictionary<Type, int> typeIdsByType = new ConcurrentDictionary<Type, int>();
      private static int typeIdCounter = 1;

      public static void Register(Type type) {
         typeIdsByType.GetOrAdd(
            type,
            add => {
               var typeId = Interlocked.Increment(ref typeIdCounter);
               TypeRegistry.RegisterType((TypeId)typeId, type);
               return typeId;
            });
      }

      public static void RegisterWithSerializer<T>(ITypeSerializer<T> serializer = null) {
         Register(typeof(T));
         if (serializer == null) {
            serializer = (ITypeSerializer<T>)AutoTypeSerializerFactory.Create(typeof(T));
         }
         typeof(TypeSerializerRegistry<>).MakeGenericType(typeof(T))
                                         .GetMethod(nameof(TypeSerializerRegistry<object>.Register))
                                         .Invoke(null, new object[] { serializer });
      }
   }
}
