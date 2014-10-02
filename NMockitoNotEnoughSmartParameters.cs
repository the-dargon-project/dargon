using System;

namespace ItzWarty.Test
{
   public class NMockitoNotEnoughSmartParameters : Exception
   {
      public NMockitoNotEnoughSmartParameters()
         : base("Did not find enough smart parameters! Number of smart parameters must be zero or method argument count.") { }
   }
}