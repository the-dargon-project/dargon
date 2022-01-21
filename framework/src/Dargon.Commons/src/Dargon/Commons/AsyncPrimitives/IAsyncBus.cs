using System;
using System.Threading.Tasks;

namespace Dargon.Commons.AsyncPrimitives {
   public interface IAsyncPoster<T> {
      Task PostAsync(T thing);
   }

   public interface IAsyncSubscriber<T> {
      Task<IDisposable> SubscribeAsync(SubscriberCallbackFunc<T> handler);
   }

   public interface IAsyncBus<T> : IAsyncPoster<T>, IAsyncSubscriber<T> { }

   public delegate Task SubscriberCallbackFunc<T>(IAsyncSubscriber<T> self, T thing);

   public static class EventBusStatics {
      public static IAsyncPoster<T> Poster<T>(this IAsyncBus<T> bus) => bus;
      public static IAsyncSubscriber<T> Subscriber<T>(this IAsyncBus<T> bus) => bus;
   }
}
