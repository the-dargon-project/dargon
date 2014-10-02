using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using Castle.DynamicProxy;
using ItzWarty.Collections;

namespace ItzWarty.Test
{
   internal class MockState
   {
      private readonly Type interfaceType;
      private readonly IDictionary<Tuple<MethodInfo, Type[], object[]>, InvocationResultTracker> trackerByArguments = new ListDictionary<Tuple<MethodInfo, Type[], object[]>, InvocationResultTracker>();
      private readonly IDictionary<Tuple<MethodInfo, Type[], object[]>, int> invocationCountsByInvocation = new Dictionary<Tuple<MethodInfo, Type[], object[]>, int>();
      private IInvocation lastInvocation;

      public MockState(Type interfaceType)
      {
         this.interfaceType = interfaceType;
      }

      public void SetInvocationResult(IInvocation invocation, IInvocationResult result)
      {
         GetInvocationResultTracker(invocation).AddResult(result);
      }

      public void HandleMockInvocation(IInvocation invocation)
      {
         this.lastInvocation = invocation;
         AddToInvocationCount(invocation, 1);
         invocation.ReturnValue = GetInvocationResultTracker(invocation).NextResult().GetValueOrThrow();
         NMockitoGlobals.SetLastInvocationAndMockState(invocation, this);
      }

      public void HandleMockVerification(IInvocation invocation, INMockitoTimesMatcher times) 
      { 
         invocation.ReturnValue = GetDefaultValue(invocation.Method.ReturnType);

         var actualInvocations = AddToInvocationCount(invocation, 0);
         times.MatchOrThrow(actualInvocations);
         AddToInvocationCount(invocation, -actualInvocations);
      }

      public void HandleMockWhenning(IInvocation invocation) { AddToInvocationCount(invocation, -1); }

      private InvocationResultTracker GetInvocationResultTracker(IInvocation invocation)
      {
         var key = new Tuple<MethodInfo, Type[], object[]>(invocation.Method, invocation.GenericArguments, invocation.Arguments);

         // Try to find our invocation result tracker... can't use dictionary comparers
         InvocationResultTracker tracker = null;
         foreach (var kvp in trackerByArguments)
         {
            if (key.Item1 == kvp.Key.Item1 &&
                ((key.Item2 == null && kvp.Key.Item2 == null) || Enumerable.SequenceEqual(key.Item2, kvp.Key.Item2)) &&
                ((key.Item3 == null && kvp.Key.Item3 == null) || Enumerable.SequenceEqual(key.Item3, kvp.Key.Item3))) {
               tracker = kvp.Value;
               break;
            }
         }

         if (tracker == null) {
            var defaultValue = GetDefaultValue(invocation.Method.ReturnType);
            trackerByArguments.Add(key, tracker = new InvocationResultTracker(defaultValue));
         } else {
         }
         return tracker;
      }

      private int AddToInvocationCount(IInvocation invocation, int delta)
      {
         var key = new Tuple<MethodInfo, Type[], object[]>(invocation.Method, invocation.GenericArguments, invocation.Arguments);
         foreach (var kvp in invocationCountsByInvocation)
         {
            if (key.Item1 == kvp.Key.Item1 &&
                ((key.Item2 == null && kvp.Key.Item2 == null) || Enumerable.SequenceEqual(key.Item2, kvp.Key.Item2)) &&
                ((key.Item3 == null && kvp.Key.Item3 == null) || Enumerable.SequenceEqual(key.Item3, kvp.Key.Item3)))
            {
               var count = kvp.Value;
               var nextCount = count + delta;
               invocationCountsByInvocation.Remove(kvp);
               invocationCountsByInvocation.Add(key, nextCount);
               return nextCount;
            }
         }

         invocationCountsByInvocation.Add(key, delta);
         return delta;
      }

      public void VerifyNoMoreInteractions()
      {
         foreach (var kvp in invocationCountsByInvocation) {
            if (kvp.Value != 0) {
               throw new VerificationTimesMismatchException(0, kvp.Value);
            }
         }
      }

      public void ClearInteractions() { invocationCountsByInvocation.Clear(); }

      private object GetDefaultValue(Type type)
      {
         if (type == typeof(void)) return null;
         return type.IsValueType ? Activator.CreateInstance(type) : null;
      }
   }
}