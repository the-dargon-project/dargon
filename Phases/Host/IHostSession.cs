﻿using Castle.Core.Internal;
using Dargon.PortableObjects.Streams;
using Dargon.Services.PortableObjects;
using Dargon.Services.Utilities;
using ItzWarty.Collections;
using ItzWarty.Networking;
using ItzWarty.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dargon.Services.Phases.Host {
   public interface IHostSession : IDisposable {
      void Start();
      Task<RemoteInvocationResult> TryRemoteInvoke(Guid serviceGuid, string methodName, object[] arguments);
   }

   public class HostSession : IHostSession {
      private readonly IHostContext hostContext;
      private readonly IConnectedSocket socket;
      private readonly IThread thread;
      private readonly ICancellationTokenSource cancellationTokenSource;
      private readonly PofStream pofStream;
      private readonly PofStreamWriter pofStreamWriter;
      private readonly PofDispatcher pofDispatcher;
      private readonly IConcurrentSet<Guid> remotelyHostedServices;
      private readonly IUniqueIdentificationSet availableInvocationIds;
      private readonly IConcurrentDictionary<uint, AsyncValueBox> invocationResponseBoxesById;

      public HostSession(
         IThreadingProxy threadingProxy,
         ICollectionFactory collectionFactory,
         PofStreamsFactory pofStreamsFactory,
         IHostContext hostContext,
         IConnectedSocket socket,
         IThread thread
      ) : this(
         collectionFactory,
         pofStreamsFactory,
         hostContext,
         socket,
         thread,
         threadingProxy.CreateCancellationTokenSource(),
         pofStreamsFactory.CreatePofStream(socket.Stream)
      ) { }

      public HostSession(
         ICollectionFactory collectionFactory,
         PofStreamsFactory pofStreamsFactory,
         IHostContext hostContext,
         IConnectedSocket socket,
         IThread thread,
         ICancellationTokenSource cancellationTokenSource,
         PofStream pofStream
      ) : this(
         hostContext,
         socket,
         thread,
         cancellationTokenSource,
         pofStream,
         pofStream.Writer,
         pofStreamsFactory.CreateDispatcher(pofStream),
         collectionFactory.CreateConcurrentSet<Guid>(),
         collectionFactory.CreateUniqueIdentificationSet(true),
         collectionFactory.CreateConcurrentDictionary<uint, AsyncValueBox>()
      ) { }

      public HostSession(IHostContext hostContext, IConnectedSocket socket, IThread thread, ICancellationTokenSource cancellationTokenSource, PofStream pofStream, PofStreamWriter pofStreamWriter, PofDispatcher pofDispatcher, IConcurrentSet<Guid> remotelyHostedServices, IUniqueIdentificationSet availableInvocationIds, IConcurrentDictionary<uint, AsyncValueBox> invocationResponseBoxesById) {
         this.hostContext = hostContext;
         this.socket = socket;
         this.thread = thread;
         this.cancellationTokenSource = cancellationTokenSource;
         this.pofStream = pofStream;
         this.pofStreamWriter = pofStreamWriter;
         this.pofDispatcher = pofDispatcher;
         this.remotelyHostedServices = remotelyHostedServices;
         this.availableInvocationIds = availableInvocationIds;
         this.invocationResponseBoxesById = invocationResponseBoxesById;
      }

      public void Initialize() {
         pofDispatcher.RegisterHandler<X2XServiceInvocation>(HandleX2XServiceInvocation);
         pofDispatcher.RegisterHandler<X2XInvocationResult>(HandleX2XInvocationResult);
         pofDispatcher.RegisterHandler<G2HServiceBroadcast>(HandleG2HServiceBroadcast);
         pofDispatcher.RegisterHandler<G2HServiceUpdate>(HandleG2HServiceUpdate);
      }

      public void Start() {
         pofDispatcher.Start();
      }

      internal void HandleX2XServiceInvocation(X2XServiceInvocation x) {
         var result = hostContext.Invoke(x.ServiceGuid, x.MethodName, x.MethodArguments);
         pofStreamWriter.WriteAsync(new X2XInvocationResult(x.InvocationId, result));
      }

      internal void HandleX2XInvocationResult(X2XInvocationResult x) {
         AsyncValueBox valueBox;
         if (invocationResponseBoxesById.TryGetValue(x.InvocationId, out valueBox)) {
            valueBox.Set(x.Payload);
         }
      }

      internal void HandleG2HServiceBroadcast(G2HServiceBroadcast x) {
         x.ServiceGuids.ForEach(remotelyHostedServices.Add);
      }

      internal void HandleG2HServiceUpdate(G2HServiceUpdate x) {
         x.AddedServiceGuids.ForEach(remotelyHostedServices.Add);
         x.RemovedServiceGuids.ForEach(guid => remotelyHostedServices.Remove(guid));
      }

      public async Task<RemoteInvocationResult> TryRemoteInvoke(Guid serviceGuid, string methodName, object[] arguments) {
         if (!remotelyHostedServices.Contains(serviceGuid)) {
            return new RemoteInvocationResult(false, null);
         } else {
            var invocationId = availableInvocationIds.TakeUniqueID();
            var asyncValueBox = invocationResponseBoxesById.GetOrAdd(invocationId, (id) => new AsyncValueBoxImpl());
            await pofStreamWriter.WriteAsync(new X2XServiceInvocation());
            var returnValue = await asyncValueBox.GetResultAsync();
            var removed = invocationResponseBoxesById.Remove(new KeyValuePair<uint, AsyncValueBox>(invocationId, asyncValueBox));
            Trace.Assert(removed, "Failed to remove AsyncValueBox from dict");
            return new RemoteInvocationResult(true, returnValue);
         }
      }

      public void Dispose() {
         cancellationTokenSource.Cancel();
         thread.Join();
         thread.Dispose();

         pofDispatcher.Dispose();
         pofStreamWriter.Dispose();
         pofStream.Dispose();
         socket.Dispose();
      }
   }

   public class RemoteInvocationResult {
      public RemoteInvocationResult(bool success, object returnValue) {
         Success = success;
         ReturnValue = returnValue;
      }

      public bool Success { get; private set; }
      public object ReturnValue { get; private set; }
   }
}