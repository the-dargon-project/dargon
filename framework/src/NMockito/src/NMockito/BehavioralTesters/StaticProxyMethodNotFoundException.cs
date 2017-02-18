using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NMockito.BehavioralTesters {
   public class StaticProxyMethodNotFoundException : Exception {
      public StaticProxyMethodNotFoundException(MethodInfo methodInfo, Type interfaceType) : base(BuildMessage(methodInfo, interfaceType)) { }

      private static string BuildMessage(MethodInfo methodInfo, Type interfaceType) {
         return "Failed to find static proxy method for " + methodInfo + " of interface " + interfaceType;
      }
   }
}
