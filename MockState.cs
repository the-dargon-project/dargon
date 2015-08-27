using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using Castle.DynamicProxy;

using Counter = System.Collections.Generic.List<int>;

namespace NMockito
{
   internal class MockState
   {
      private readonly Type interfaceType;
      private readonly List<KeyValuePair<Tuple<MethodInfo, Type[], INMockitoSmartParameter[]>, InvocationResultTracker>> trackerByArguments = new List<KeyValuePair<Tuple<MethodInfo, Type[], INMockitoSmartParameter[]>, InvocationResultTracker>>();
      private readonly List<KeyValuePair<Tuple<MethodInfo, Type[], object[]>, Counter>> invocationCountsByInvocation = new List<KeyValuePair<Tuple<MethodInfo, Type[], object[]>, Counter>>();
      private IInvocation lastInvocation;

      public MockState(Type interfaceType)
      {
         this.interfaceType = interfaceType;
      }

      public void AddInvocationExecutor(IInvocation invocation, INMockitoSmartParameter[] smartParameters, IInvocationExecutor executor)
      {
         var method = invocation.Method;
         var genericArguments = invocation.GenericArguments;
         var methodParameters = method.GetParameters();
         var invocationArguments = invocation.Arguments;

         // Flatten params[] of invocation arguments
         if (methodParameters.Length > 0 && 
             methodParameters.Last().GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0 &&
             invocationArguments.Last() != null) {
            var paramsArray = (Array)invocationArguments.Last();
            var newInvocationArguments = new object[invocationArguments.Length + paramsArray.Length - 1];
            var cutIndex = invocationArguments.Length - 1;
            Array.Copy(invocationArguments, newInvocationArguments, cutIndex);
            Array.Copy(paramsArray, 0, newInvocationArguments, cutIndex, paramsArray.Length);
            invocationArguments = newInvocationArguments;
         }

         // Convert all invocation arguments into eq(arg[i]) smart parameters
         if (smartParameters.Length == 0)
            smartParameters = ConvertInvocationArgumentsToEqualityParameters(invocationArguments);

         // Find out/ref parameters, swap them with null and record the index => smart parameter replacement.
         var refReplacementsByIndex = new List<KeyValuePair<int, object>>();
         for (var i = 0; i < methodParameters.Length; i++) {
            var parameter = methodParameters[i];
            if (parameter.Attributes.HasFlag(ParameterAttributes.Out)) {
               var replacement = invocation.Arguments[i];
               smartParameters[i] = null;
               refReplacementsByIndex.Add(new KeyValuePair<int, object>(i, replacement));
            }
         }

         var tracker = trackerByArguments.FirstOrDefault(kvp => kvp.Key.Item1 == method && kvp.Key.Item2 == genericArguments && SmartParametersEqual(kvp.Key.Item3, smartParameters)).Value;
         if (tracker == null) {
            tracker = new InvocationResultTracker(GetDefaultValue(method.ReturnType), refReplacementsByIndex);
            var key = new Tuple<MethodInfo, Type[], INMockitoSmartParameter[]>(method, genericArguments, smartParameters);
            trackerByArguments.Add(new KeyValuePair<Tuple<MethodInfo, Type[], INMockitoSmartParameter[]>, InvocationResultTracker>(key, tracker));
         }
         tracker.AddResult(executor);
      }

      private bool SmartParametersEqual(INMockitoSmartParameter[] a, INMockitoSmartParameter[] b) {
         if (a.Length != b.Length) {
            return false;
         }
         for (var i = 0; i < a.Length; i++) {
            if (!Equals(a[i], b[i])) {
               return false;
            }
         }
         return true;
      }

      public void HandleMockInvocation(IInvocation invocation)
      {
         this.lastInvocation = invocation;
         GetInvocationCounter(invocation).Add(NMockitoInvocationCounters.TakeNextInvocationCounter());
         invocation.ReturnValue = GetInvocationResult(invocation);
         NMockitoGlobals.SetLastInvocationAndMockState(invocation, this);
      }

      public void DecrementInvocationCounter(IInvocation whenBodyInvocation) 
      { 
         // Rollback when(mock.Method(params)) invocation (mock.Method(params))
         var counter = GetInvocationCounter(whenBodyInvocation); 
         counter.RemoveAt(counter.Count - 1);
      }

      public void HandleMockVerification(IInvocation invocation, INMockitoTimesMatcher times, NMockitoOrder order) 
      { 
         invocation.ReturnValue = GetDefaultValue(invocation.Method.ReturnType);

         // Flatten params[] of invocation arguments
         var method = invocation.Method;
         var methodParameters = method.GetParameters();
         var invocationArguments = invocation.Arguments;
         if (methodParameters.Length > 0 &&
             methodParameters.Last().GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0 &&
             invocationArguments.Last() != null) {
            var paramsArray = (Array)invocationArguments.Last();
            var newInvocationArguments = new object[invocationArguments.Length + paramsArray.Length - 1];
            var cutIndex = invocationArguments.Length - 1;
            Array.Copy(invocationArguments, newInvocationArguments, cutIndex);
            Array.Copy(paramsArray, 0, newInvocationArguments, cutIndex, paramsArray.Length);
            invocationArguments = newInvocationArguments;
         }

         var smartParameters = NMockitoSmartParameters.CopyAndClearSmartParameters().ToArray();
         if (smartParameters.Length == 0) {
            smartParameters = ConvertInvocationArgumentsToEqualityParameters(invocationArguments);
         }
         if (smartParameters.Length != invocationArguments.Length) {
            throw new NMockitoNotEnoughSmartParameters();
         }
         // Find out/ref parameters, swap them with null 
         for (var i = 0; i < methodParameters.Length; i++) {
            var parameter = methodParameters[i];
            if (parameter.Attributes.HasFlag(ParameterAttributes.Out)) {
               smartParameters[i] = null;
            }
         }

         var counters = FindMatchingInvocationCounters(invocation.Method, invocation.GenericArguments, smartParameters);
         times.MatchOrThrow(counters.Sum(counter => counter.Count), invocation, invocationCountsByInvocation);
         foreach (var counter in counters) {
            foreach (var count in counter) {
               NMockitoInvocationCounters.AcceptVerificationCounterOrThrow(count, order);
            }
            counter.Clear();
         }
      }

      private object GetInvocationResult(IInvocation invocation) {
         // Flatten params[] of invocation arguments
         var method = invocation.Method;
         var methodParameters = method.GetParameters();
         var invocationArguments = invocation.Arguments;
         if (methodParameters.Length > 0 &&
             methodParameters.Last().GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0 &&
             invocationArguments.Last() != null) {
            var paramsArray = (Array)invocationArguments.Last();
            var newInvocationArguments = new object[invocationArguments.Length + paramsArray.Length - 1];
            var cutIndex = invocationArguments.Length - 1;
            Array.Copy(invocationArguments, newInvocationArguments, cutIndex);
            Array.Copy(paramsArray, 0, newInvocationArguments, cutIndex, paramsArray.Length);
            invocationArguments = newInvocationArguments;
         }

         // Try to find our invocation result tracker... can't use dictionary comparers
         InvocationResultTracker tracker = null;
         foreach (var kvp in trackerByArguments)
         {
            if (invocation.Method == kvp.Key.Item1 &&
                ((invocation.GenericArguments == null && kvp.Key.Item2 == null) || Enumerable.SequenceEqual(invocation.GenericArguments, kvp.Key.Item2)) &&
                ((invocationArguments == null && kvp.Key.Item3 == null) || invocationArguments.Length == kvp.Key.Item3.Length))
            {
               bool invocationMatching = true;
               for (var i = 0; i < invocationArguments.Length && invocationMatching; i++) {
                  invocationMatching &= kvp.Key.Item3[i] == null || kvp.Key.Item3[i].Test(invocationArguments[i]);
               }
               if (invocationMatching) {
                  tracker = kvp.Value;
                  break;
               }
            }
         }

         var returnValue = GetDefaultValue(invocation.Method.ReturnType);
         if (tracker != null) {
            // replace smart parameter with their placeholders
            var refReplacementsByIndex = tracker.RefReplacementsByIndex;
            foreach (var kvp in refReplacementsByIndex) {
               invocation.Arguments[kvp.Key] = kvp.Value;
            }
            var currentExecutor = tracker.NextResult();
            while (true) {
               returnValue = currentExecutor.Execute(invocation);
               if (currentExecutor.IsTerminal) {
                  break;
               } else {
                  currentExecutor = tracker.NextResult();
               }
            } 

            // replace smart parameters' placeholders with default value, if they're not swapped out
            foreach (var kvp in refReplacementsByIndex) {
               if (ReferenceEquals(invocation.Arguments[kvp.Key], kvp.Value)) {
                  var parameterType = invocation.Method.GetParameters()[kvp.Key].ParameterType;
                  // If we have an 'out int', for example, the type is actually a by-ref int&.
                  if (parameterType.IsByRef) {
                     parameterType = parameterType.GetElementType();
                  }
                  if (parameterType.IsValueType) {
                     invocation.Arguments[kvp.Key] = Activator.CreateInstance(parameterType);
                  } else {
                     invocation.Arguments[kvp.Key] = null;
                  }
               }
            }
         }
         return returnValue;
      }

      private INMockitoSmartParameter[] ConvertInvocationArgumentsToEqualityParameters(object[] invocationArguments) {
         var results = new INMockitoSmartParameter[invocationArguments.Length];
         for (var i = 0; i < invocationArguments.Length; i++) {
            results[i] = new NMockitoEquals(invocationArguments[i]);
         }
         return results;
      }

      private List<int> GetInvocationCounter(IInvocation invocation) {
         var method = invocation.Method;
         var methodParameters = method.GetParameters();
         var invocationArguments = invocation.Arguments;

         // Flatten params[] of invocation arguments
         if (methodParameters.Length > 0 &&
             methodParameters.Last().GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0 &&
             invocationArguments.Last() != null) {
            var paramsArray = (Array)invocationArguments.Last();
            var newInvocationArguments = new object[invocationArguments.Length + paramsArray.Length - 1];
            var cutIndex = invocationArguments.Length - 1;
            Array.Copy(invocationArguments, newInvocationArguments, cutIndex);
            Array.Copy(paramsArray, 0, newInvocationArguments, cutIndex, paramsArray.Length);
            invocationArguments = newInvocationArguments;
         }

         foreach (var kvp in invocationCountsByInvocation) {
            if (invocation.Method == kvp.Key.Item1 &&
                ((invocation.GenericArguments == null && kvp.Key.Item2 == null) || invocation.GenericArguments.SequenceEqual(kvp.Key.Item2)) &&
                invocationArguments.SequenceEqual(kvp.Key.Item3)) {
               return kvp.Value;
            }
         }

         var counter = new Counter();
         invocationCountsByInvocation.Add(new KeyValuePair<Tuple<MethodInfo, Type[], object[]>, Counter>(new Tuple<MethodInfo, Type[], object[]>(invocation.Method, invocation.GenericArguments, invocationArguments), counter));
         return counter;
      }

      private List<Counter> FindMatchingInvocationCounters(MethodInfo method, Type[] genericArguments, INMockitoSmartParameter[] smartParameters)
      {
         var results = new List<Counter>();
         foreach (var kvp in invocationCountsByInvocation) {
            if (method == kvp.Key.Item1 &&
                ((genericArguments == null && kvp.Key.Item2 == null) || genericArguments.SequenceEqual(kvp.Key.Item2)) &&
                smartParameters.Length == kvp.Key.Item3.Length) {
               bool matching = true;
               for (var i = 0; i < smartParameters.Length && matching; i++) {
                  matching &= smartParameters[i] == null || kvp.Key.Item3[i] == null || smartParameters[i].Test(kvp.Key.Item3[i]);
               }
               if (matching) {
                  results.Add(kvp.Value);
               }
            }
         }
         return results;
      }

      public void VerifyNoMoreInteractions()
      {
         List<Exception> exceptions = new List<Exception>();
         foreach (var kvp in invocationCountsByInvocation) {
            if (kvp.Value.Count != 0) {
               exceptions.Add(new VerificationTimesMismatchException("no more", kvp.Value.Count, null, invocationCountsByInvocation));
            }
         }
         if (exceptions.Any()) {
            throw new AggregateException(exceptions);
         }
      }

      public void ClearInteractions() { invocationCountsByInvocation.Clear(); }

      public void ClearInteractions(int expectedCount)
      {
         var actualCount = invocationCountsByInvocation.Sum(kvp => kvp.Value.Count);
         if (expectedCount != actualCount) {
            throw new VerificationTimesMismatchException(expectedCount.ToString(), actualCount, null, invocationCountsByInvocation);
         } else {
            invocationCountsByInvocation.Clear();
         }
      }

      private object GetDefaultValue(Type type)
      {
         if (type == typeof(void)) return null;
         return type.IsValueType ? Activator.CreateInstance(type) : null;
      }
   }
}