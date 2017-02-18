using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Castle.Core.Internal;
using Castle.DynamicProxy;
using NMockito.SmartParameters;
using NMockito.Transformations;

namespace NMockito.Mocks {
   public class InvocationDescriptor : IEquatable<InvocationDescriptor> {
      public MockInterceptor Interceptor { get; set; }
      public MethodInfo Method { get; set; }
      public IInvocation Invocation { get; set; }
      public Type MockedType => Method.DeclaringType;
      public object Mock => Invocation.Proxy;
      public object Target => Invocation.InvocationTarget;

      public object[] Arguments { get; set; }
      public List<InvocationTransformation> Transformations { get; set; }
      public SmartParameterCollection SmartParameters { get; set; }
      public Exception Exception { get; set; }

      public InvocationDescriptor Clone() {
         return new InvocationDescriptor {
            Interceptor = Interceptor,
            Method = Method,
            Invocation = Invocation,
            Arguments = (object[])Arguments.Clone(),
            Transformations = new List<InvocationTransformation>(Transformations),
            SmartParameters = SmartParameters,
            Exception = Exception
         };
      }

      public override int GetHashCode() {
         int hash = 13;
         hash += 17 * Method.GetHashCode();
         foreach (var argument in Arguments) {
            hash += 17 * (argument?.GetHashCode() ?? 0);
         }
         return hash;
      }

      public override bool Equals(object obj) {
         return (obj as InvocationDescriptor)?.Equals(this) ?? false;
      }

      public bool Equals(InvocationDescriptor other) {
         return other.Interceptor == this.Interceptor &&
                other.Method.Equals(this.Method) &&
                other.Arguments.SequenceEqual(this.Arguments) &&
                other.Mock == this.Mock;
      }

      public override string ToString() {
         var sb = new StringBuilder();
         sb.Append(MockedType.FullName);
         sb.Append(' ');
         sb.Append(Method.Name);

         var genericArguments = Method.GetGenericArguments();
         if (genericArguments.Any()) {
            sb.Append('<');
            genericArguments.ToList().ForEach(t => sb.Append(t.FullName));
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