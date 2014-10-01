using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using ItzWarty.Collections;

namespace ItzWarty.Test
{
   internal class MockState
   {
      private readonly Type interfaceType;
      private readonly IDictionary<Tuple<MethodInfo, Type[], object[]>, InvocationResultTracker> trackerByArguments = new ListDictionary<Tuple<MethodInfo, Type[], object[]>, InvocationResultTracker>();
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
         invocation.ReturnValue = GetInvocationResultTracker(invocation).NextResult().GetValueOrThrow();
         NMockitoGlobals.SetLastInvocationAndMockState(invocation, this);
      }

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

      private object GetDefaultValue(Type type)
      {
         if (type == typeof(void)) return null;
         return type.IsValueType ? Activator.CreateInstance(type) : null;
      }
   }
}