using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Dargon.Commons.AsyncPrimitives {
   public class GlobalAsyncLocal2_t<T, TNamespaceUniqueType, TSlotUniqueType> where TNamespaceUniqueType : struct where TSlotUniqueType : struct {
      public GlobalAsyncLocal2_t() => throw new InvalidOperationException();

      public static T Value {
         get => Get();
         set => Set(value);
      }

      public static T Get() {
         var slotIndex = AsyncLocal2SlotCounter.For<TNamespaceUniqueType, TSlotUniqueType>.SlotIndex;
         var store = AsyncLocal2State.AlsStore.Value;
         ref var slot = ref store[slotIndex];
         if (slot == null) return default;
         return (T)slot;
      }

      public static void Set(T value) {
         var slotIndex = AsyncLocal2SlotCounter.For<TNamespaceUniqueType, TSlotUniqueType>.SlotIndex;
         var temp = AsyncLocal2State.AlsStore.Value;
         temp[slotIndex] = value;
         AsyncLocal2State.AlsStore.Value = temp;
      }
   }

   public static class AsyncLocal2State {
      public static readonly AsyncLocal<AsyncLocal2Store> AlsStore = new();
   }

   [System.Runtime.CompilerServices.InlineArray(Length)]
   public struct AsyncLocal2Store {
      private object element;
      public const int Length = 8;
   }

   public static class AsyncLocal2SlotCounter {
      private static int NextSlotIndex;

      public static class For<TNamespaceUniqueType, TSlotUniqueType> where TNamespaceUniqueType : struct where TSlotUniqueType : struct {
         public static readonly int SlotIndex = Interlocked2.PostIncrement(ref NextSlotIndex);
      }
   }

   public struct DargonAls2Namespace_t;
}
