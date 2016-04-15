using System;
using System.Threading.Tasks;

namespace Dargon.Courier {
   public interface IAsyncConsumer<T> {
      Task PostAsync(T thing);
   }

   public interface IAsyncProducer<T> {
      void Subscribe(Func<IAsyncProducer<T>, T, Task> handler);
   }

   public interface IAsyncEventBus<T> : IAsyncConsumer<T>, IAsyncProducer<T> { }

   public static class EventBusStatics {
      public static IAsyncConsumer<T> Consumer<T>(this IAsyncEventBus<T> bus) => bus;
      public static IAsyncProducer<T> Producer<T>(this IAsyncEventBus<T> bus) => bus;
   }
}
