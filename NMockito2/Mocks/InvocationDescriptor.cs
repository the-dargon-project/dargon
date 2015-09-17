using Castle.Core.Internal;
using Castle.DynamicProxy;
using NMockito2.SmartParameters;
using NMockito2.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NMockito2.Mocks {
   public class InvocationDescriptor {
      public MethodInfo Method { get; set; }
      public object[] Arguments { get; set; }
      public IInvocation Invocation { get; set; }
      public List<InvocationTransformation> Transformations { get; set; }
      public SmartParameterCollection SmartParameters { get; set; }
      public Exception Exception { get; set; }

      public object Mock => Invocation.Proxy;
      public object Target => Invocation.InvocationTarget;
      public Type MockedType => Invocation.Method.ReflectedType;

      public InvocationDescriptor Clone() {
         return new InvocationDescriptor {
            Method = Method,
            Arguments = (object[])Arguments.Clone(),
            Invocation = Invocation,
            Transformations = new List<InvocationTransformation>(Transformations),
            SmartParameters = SmartParameters,
            Exception = Exception
         };
      }

      public override string ToString() {
         var sb = new StringBuilder();
         sb.Append(MockedType.FullName);
         sb.Append(' ');
         sb.Append(Method.Name);

         var genericArguments = Method.GetGenericArguments();
         if (genericArguments.Any()) {
            sb.Append('<');
            genericArguments.ForEach(t => sb.Append(t.FullName));
            sb.Append('>');
         }

         sb.Append("(");
         var parameters = Method.GetParameters();
         for (var i = 0; i < parameters.Length; i++) {
            if (i != 0) {
               sb.Append(", ");
            }

            var parameter = parameters[i];
            var argument = Arguments[i];
            sb.Append(parameter.ParameterType.FullName);
            sb.Append(' ');
            sb.Append(parameter.Name);
            sb.Append(" = ");
            sb.Append(argument?.ToString() ?? "null");
         }
         sb.Append(")");
         return sb.ToString();
      }
   }
}