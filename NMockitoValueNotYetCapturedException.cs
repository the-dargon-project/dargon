using System;

namespace ItzWarty.Test
{
   public class NMockitoValueNotYetCapturedException : Exception
   {
      public NMockitoValueNotYetCapturedException() : base("The value has not yet been captured") { }
   }
}