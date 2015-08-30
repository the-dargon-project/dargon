using System;
using Dargon.PortableObjects;
using ItzWarty.Collections;
using SCG = System.Collections.Generic;

namespace Dargon.Ryu {
   public class RyuFactory {
      public RyuContainer Create() {
         var pofContext = new PofContext();
         var pofSerializer = new PofSerializer(pofContext);
         return new RyuContainerImpl(
            pofContext, 
            pofSerializer,
            new SCG.Dictionary<Type, RyuPackageV1TypeInfo>(),
            new ConcurrentDictionary<Type, object>(),
            new HashSet<Type>());
      }
   }
}