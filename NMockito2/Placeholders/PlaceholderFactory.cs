using NMockito2.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NMockito2.Placeholders {
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
            return Array.CreateInstance(type.GetElementType(), 1337 + counter);
         } else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
            var genericArgument = type.GetGenericArguments()[0];
            return CreatePlaceholder(genericArgument.MakeArrayType());
         } else if (type.IsClass && type != typeof(string)) {
            var ctor = type.GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 0);
            return ctor?.Invoke(null);
         } else {
            var counter = Interlocked.Increment(ref placeholderCounter);
            switch (type.Name) {
               case nameof(String): return "placeholder_" + counter;
               case nameof(Char): return (char)('!' + (counter % 94)); // ! to ~ in ascii
               case nameof(Byte): return (byte)(1 + counter % 254);
               case nameof(SByte): return (sbyte)(1 + counter % 254);
               case nameof(UInt16): return (ushort)(1 + counter % 65535);
               case nameof(Int16): return (short)(1 + counter % 65535);
               case nameof(UInt32): return (uint)(1 + counter % (UInt32.MaxValue - 1));
               case nameof(Int32): return (int)(1 + counter % (UInt32.MaxValue - 1));
               case nameof(UInt64): return (ulong)(1 + counter);
               case nameof(Int64): return (long)(1 + counter);
               case nameof(Boolean): return counter % 2 == 0;
               case nameof(Guid): return new Guid(1 + counter, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }
            throw new NotSupportedException("NMockito does not support creating placeholders for type " + type.FullName);
         }
      }
   }
}
