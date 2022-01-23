using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.Channels {
   public interface Channel<T> : ReadableChannel<T>, WritableChannel<T> { }

   public interface CountableChannel {
      int Count { get; }
   }

   public interface ReadableChannel<T> : CountableChannel {
      bool TryRead(out T message);
      Task<T> ReadAsync(CancellationToken cancellationToken = default, Func<T, bool> acceptanceTest = null);
   }

   public interface WritableChannel<T> : CountableChannel {
      Task WriteAsync(T message, CancellationToken cancellationToken);
   }
}
