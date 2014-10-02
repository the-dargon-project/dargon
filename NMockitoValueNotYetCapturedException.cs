using System;

namespace NMockito
{
   public class NMockitoValueNotYetCapturedException : Exception
   {
      public NMockitoValueNotYetCapturedException() : base("The value has not yet been captured") { }
   }
}