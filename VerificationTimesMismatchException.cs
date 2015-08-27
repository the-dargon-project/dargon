using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Castle.DynamicProxy;

namespace NMockito
{
   internal class VerificationTimesMismatchException : Exception
   {
      public VerificationTimesMismatchException(string expectedCountString, int actual, IInvocation invocation, List<KeyValuePair<Tuple<MethodInfo, Type[], object[]>, List<int>>> invocationCountsByInvocation)
         : base(GetMessage(expectedCountString, actual, invocation, invocationCountsByInvocation)) { }

      private static string GetMessage(string expectedCountString, int actual, IInvocation invocation, List<KeyValuePair<Tuple<MethodInfo, Type[], object[]>, List<int>>> invocationCountsByInvocation) {
         var invocationString = "of any method";
         if (invocation != null) {
            invocationString = "of " + GetInvocationSummary(invocation.Method, invocation.GenericArguments, invocation.Arguments);
         }

         var sb = new StringBuilder();
         sb.AppendLine($"Expected {expectedCountString} invocations {invocationString} but found {actual} invocations.");
         sb.AppendLine($"Other invocations found:");
         foreach (var kvp in invocationCountsByInvocation) {
            sb.AppendLine($"{kvp.Value.Count} invocations of " + GetInvocationSummary(kvp.Key.Item1, kvp.Key.Item2, kvp.Key.Item3));
         }
         return sb.ToString();
      }

      private static string GetInvocationSummary(MethodInfo method, Type[] genericArguments, object[] arguments) {
         var methodName = method.Name;
         var methodParameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
         var genericString = "";
         if (genericArguments != null && genericArguments.Length > 0) {
            genericString = "<" + string.Join(", ", genericArguments.Select(x => x.Name)) + ">";
         }
         var parameterStrings = methodParameterTypes.Zip(arguments, (type, value) => $"{type.Name} {value}");
         if (method.GetParameters().Length != arguments.Length) {
            var paramsString = "params " + methodParameterTypes.Last().Name + " { " + string.Join(", ", arguments.Skip(methodParameterTypes.Length - 1).Select(x => x.ToString())) + " }";
            parameterStrings = parameterStrings.Take(methodParameterTypes.Length - 1);
            parameterStrings = parameterStrings.Concat(new[] { paramsString });
         }
         var argsString = string.Join(", ", parameterStrings);
         return $"{methodName}{genericString}( {argsString} )";
      }
   }
}