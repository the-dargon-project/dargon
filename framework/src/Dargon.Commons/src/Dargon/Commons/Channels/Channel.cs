﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Commons.Channels {
   public interface Channel<T> : ReadableChannel<T>, WritableChannel<T> { }

   public interface CountableChannel {
      int Count { get; }
   }

   public interface ReadableChannel<T> : CountableChannel {
      bool TryRead(out T message);
      Task<T> ReadAsync(CancellationToken cancellationToken, Func<T, bool> acceptanceTest);
   }

   public interface WritableChannel<T> : CountableChannel {
      Task WriteAsync(T message, CancellationToken cancellationToken);
   }
}
