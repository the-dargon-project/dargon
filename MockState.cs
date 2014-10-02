using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using Castle.DynamicProxy;
using ItzWarty.Collections;

using TrackerByArgumentsKey = System.Tuple<System.Reflection.MethodInfo, System.Type[], ItzWarty.Test.INMockitoSmartParameter[]>;
using CountersByInvocationKey = System.Tuple<System.Reflection.MethodInfo, System.Type[], object[]>;

namespace ItzWarty.Test
{
   internal class MockState
   {
      private readonly Type interfaceType;
      private readonly List<KeyValuePair<TrackerByArgumentsKey, InvocationResultTracker>> trackerByArguments = new List<KeyValuePair<TrackerByArgumentsKey, InvocationResultTracker>>();
      private readonly List<KeyValuePair<CountersByInvocationKey, Counter>> invocationCountsByInvocation = new List<KeyValuePair<CountersByInvocationKey, Counter>>();
      private IInvocation lastInvocation;

      public MockState(Type interfaceType)
      {
         this.interfaceType = interfaceType;
      }

      public void SetInvocationResult(IInvocation invocation, INMockitoSmartParameter[] smartParameters, IInvocationResult result)
      {
         var method = invocation.Method;
         var genericArguments = invocation.GenericArguments;
         var parameters = smartParameters;

         if (parameters.Length != invocation.Arguments.Length)
            parameters = ConvertInvocationArgumentsToEqualityParameters(invocation.Arguments);

         var tracker = trackerByArguments.FirstOrDefault(kvp => kvp.Key.Item1 == method && kvp.Key.Item2 == genericArguments && SmartParametersEqual(kvp.Key.Item3, parameters)).Value;
         if (tracker == null) {
            tracker = new InvocationResultTracker(GetDefaultValue(method.ReturnType));
            var key = new TrackerByArgumentsKey(method, genericArguments, parameters);
            trackerByArguments.Add(new KeyValuePair<TrackerByArgumentsKey, InvocationResultTracker>(key, tracker));
         }
         tracker.AddResult(result);
      }

      private bool SmartParametersEqual(INMockitoSmartParameter[] a, INMockitoSmartParameter[] b) {
         if (a.Length != b.Length) {
            return false;
         }
         for (var i = 0; i < a.Length; i++) {
            if (!a[i].Equals(b[i])) {
               return false;
            }
         }
         return true;
      }

      public void HandleMockInvocation(IInvocation invocation)
      {
         this.lastInvocation = invocation;
         GetInvocationCounter(invocation).Count++;
         invocation.ReturnValue = GetInvocationResult(invocation);
         NMockitoGlobals.SetLastInvocationAndMockState(invocation, this);
      }

      public void DecrementInvocationCounter(IInvocation whenBodyInvocation) 
      { 
         // Rollback when(mock.Method(params)) invocation (mock.Method(params))
         GetInvocationCounter(whenBodyInvocation).Count--; 
      }

      public void HandleMockVerification(IInvocation invocation, INMockitoTimesMatcher times) 
      { 
         invocation.ReturnValue = GetDefaultValue(invocation.Method.ReturnType);

         var smartParameters = NMockitoSmartParameters.CopyAndClearSmartParameters().ToArray();
         if (smartParameters.Length == 0) {
            smartParameters = ConvertInvocationArgumentsToEqualityParameters(invocation.Arguments);
         }
         if (smartParameters.Length != invocation.Arguments.Length) {
            throw new NMockitoNotEnoughSmartParameters();
         }

         var counters = FindMatchingInvocationCounters(invocation.Method, invocation.GenericArguments, smartParameters);
         times.MatchOrThrow(counters.Sum(counter => counter.Count));
         foreach (var counter in counters) {
            counter.Count = 0;
         }
      }

      private object GetInvocationResult(IInvocation invocation)
      {
         // Try to find our invocation result tracker... can't use dictionary comparers
         InvocationResultTracker tracker = null;
         foreach (var kvp in trackerByArguments)
         {
            if (invocation.Method == kvp.Key.Item1 &&
                ((invocation.GenericArguments == null && kvp.Key.Item2 == null) || Enumerable.SequenceEqual(invocation.GenericArguments, kvp.Key.Item2)) &&
                ((invocation.Arguments == null && kvp.Key.Item3 == null) || invocation.Arguments.Length == kvp.Key.Item3.Length))
            {
               bool invocationMatching = true;
               for (var i = 0; i < invocation.Arguments.Length && invocationMatching; i++) {
                  invocationMatching &= kvp.Key.Item3[i].Test(invocation.Arguments[i]);
               }
               tracker = kvp.Value;
               break;
            }
         }
         if (tracker != null) {
            return tracker.NextResult().GetValueOrThrow();
         } else {
            return GetDefaultValue(invocation.Method.ReturnType);
         }
      }

      private INMockitoSmartParameter[] ConvertInvocationArgumentsToEqualityParameters(object[] arguments)
      {
         return Util.Generate(arguments.Length, i => (INMockitoSmartParameter)new NMockitoEquals(arguments[i]));
      }

      private Counter GetInvocationCounter(IInvocation invocation)
      {
         foreach (var kvp in invocationCountsByInvocation) {
            if (invocation.Method == kvp.Key.Item1 &&
                ((invocation.GenericArguments == null && kvp.Key.Item2 == null) || invocation.GenericArguments.SequenceEqual(kvp.Key.Item2)) &&
                invocation.Arguments.SequenceEqual(kvp.Key.Item3)) {
               return kvp.Value;
            }
         }

         var counter = new Counter();
         invocationCountsByInvocation.Add(new KeyValuePair<CountersByInvocationKey, Counter>(new CountersByInvocationKey(invocation.Method, invocation.GenericArguments, invocation.Arguments), counter));
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
                  matching &= smartParameters[i].Test(kvp.Key.Item3[i]);
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
         foreach (var kvp in invocationCountsByInvocation) {
            if (kvp.Value.Count != 0) {
               throw new VerificationTimesMismatchException(0, kvp.Value.Count);
            }
         }
      }

      public void ClearInteractions() { invocationCountsByInvocation.Clear(); }

      private object GetDefaultValue(Type type)
      {
         if (type == typeof(void)) return null;
         return type.IsValueType ? Activator.CreateInstance(type) : null;
      }

      private class Counter
      {
         public Counter(int count = 0) { this.Count = count; }
         public int Count { get; set; }
      }
   }
}