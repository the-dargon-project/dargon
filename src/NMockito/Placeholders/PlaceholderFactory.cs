using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NMockito.Mocks;

namespace NMockito.Placeholders {
   public class PlaceholderFactory {
      private readonly MockFactory mockFactory;
      private int placeholderCounter = -1;

      public PlaceholderFactory(MockFactory mockFactory) {
         this.mockFactory = mockFactory;
      }

      public T CreatePlaceholder<T>() => (T)CreatePlaceholder(typeof(T));

      public object CreatePlaceholder(Type type) {
         if (type.IsArray) {
            var counter = Interlocked.Increment(ref placeholderCounter);
            var size = (13 * counter) % 7 + 3;
            var elementType = type.GetElementType();
            var array = Array.CreateInstance(elementType, size);
            for (var i = 0; i < size; i++) {
               array.SetValue(CreatePlaceholder(elementType), i);
            }
            return array;
         } else if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type)) {
            var enumerableType = type.GetInterfaces().First(i => i.Name.Contains(nameof(IEnumerable)) && i.IsGenericType);
            var elementType = enumerableType.GetGenericArguments()[0];
            var enumerableConstrutor = type.GetConstructor(new[] { enumerableType });
            if (enumerableConstrutor != null) {
               return Activator.CreateInstance(type, CreatePlaceholder(elementType.MakeArrayType()));
            }
            var instance = Activator.CreateInstance(type);
            var add = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                          .FirstOrDefault(m => m.Name.Contains("Add") && m.GetParameters().Length == 1) ?? type.GetMethod("Enqueue");
            foreach (var element in (Array)CreatePlaceholder(elementType.MakeArrayType())) {
               add.Invoke(instance, new[] { element });
            }
            return instance;
         } else if (type.IsClass && type != typeof(string)) {
            var ctor = type.GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 0);
            return ctor?.Invoke(null);
         } else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
            var genericArgs = type.GetGenericArguments();
            var keyType = genericArgs[0];
            var valueType = genericArgs[1];
            return Activator.CreateInstance(type, CreatePlaceholder(keyType), CreatePlaceholder(valueType));
         } else {
            var counter = Interlocked.Increment(ref placeholderCounter);
            switch (type.Name) {
               case nameof(String): return "placeholder_" + (counter + 1);
               case nameof(Char): return (char)('!' + (counter % 94)); // ! to ~ in ascii
               case nameof(Byte): return (byte)(1 + counter % 254);
               case nameof(SByte): return (sbyte)(1 + counter % 254);
               case nameof(UInt16): return (ushort)(1 + counter % 65535);
               case nameof(Int16): return (short)(1 + counter % 65535);
               case nameof(UInt32): return (uint)(1 + counter % (UInt32.MaxValue - 1));
               case nameof(Int32): return (int)(1 + counter % (UInt32.MaxValue - 1));
               case nameof(UInt64): return (ulong)(1 + counter);
               case nameof(Int64): return (long)(1 + counter);
               case nameof(Single): return (float)(1 + counter);
               case nameof(Double): return (double)(1 + counter);
               case nameof(Boolean): return counter % 2 == 0;
               case nameof(Guid): return new Guid(1 + counter, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }
            throw new NotSupportedException("NMockito does not support creating placeholders for type " + type.FullName);
         }
      }
   }
}
