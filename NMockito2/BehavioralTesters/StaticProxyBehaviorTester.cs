using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using NMockito2.Fluent;
using NMockito2.Mocks;
using NMockito2.Utilities;

namespace NMockito2.BehavioralTesters {
   public class StaticProxyBehaviorTester {
      private static readonly CombinatorialSetGenerator combinatorialSetGenerator = new PairwiseCombinatorialSetGenerator();
      private readonly NMockitoInstance nmockito;

      public StaticProxyBehaviorTester(NMockitoInstance nmockito) {
         this.nmockito = nmockito;
      }

      public void TestStaticProxy(Type staticClass) {
         var mockableFields = staticClass.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                                         .Where(f => f.FieldType.IsInterface || f.FieldType.IsClass);
         foreach (var f in mockableFields) {
            Debug.WriteLine("Testing proxy to field " + f.Name + " of type " + f.FieldType + ": ");
            TestStaticProxyField(staticClass, f);
         }
      }

      public void TestStaticProxyField(Type staticClass, FieldInfo staticField) {
         var interfaceType = staticField.FieldType;
         var interfaceMethods = interfaceType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                             .Where(interfaceMethod => interfaceMethod.DeclaringType != typeof(object));
         var staticMethods = staticClass.GetMethods(BindingFlags.Public | BindingFlags.Static);
         foreach (var method in interfaceMethods) {
            var staticMethodMatches = staticMethods.Where(m => m.Name.Equals(method.Name) && m.ReturnType.IsEqualTo(method.ReturnType)).ToList();
            if (staticMethodMatches.Count == 0) {
               throw new EntryPointNotFoundException("Failed to find static proxy method for " + method + " of interface " + interfaceType);
            } else if (staticMethodMatches.Count > 1) {
               throw new AmbiguousMatchException("Found more than one static proxy method matching " + method);
            } else {
               var staticMethod = staticMethodMatches[0];
               if (staticMethod.IsGenericMethodDefinition) {
                  TestProxiedStaticGenericMethodDefinition(staticField, method, staticMethod);
               } else {
                  TestProxiedStaticMethod(staticField, method, staticMethod);
               }
            }
         }
      }

      private void TestProxiedStaticGenericMethodDefinition(FieldInfo staticField, MethodInfo method, MethodInfo staticMethod) {
         var genericArgumentOptions = new[] {
            typeof(string), typeof(char),
            typeof(object), typeof(long), typeof(int), typeof(uint), typeof(short), typeof(byte),
            typeof(bool), typeof(Guid),

            typeof(string[]),
            typeof(object[]), typeof(long[]), typeof(int[]), typeof(uint[]), typeof(byte[]),
            typeof(bool[]), typeof(Guid[])
         };
         var genericParameters = staticMethod.GetGenericArguments();
         var genericArgumentsByParameterIndex = new Type[genericParameters.Length][];
         for (var i = 0; i < genericParameters.Length; i++) {
            var genericParameter = genericParameters[i];
            var genericArguments = genericArgumentOptions;
            foreach (var constraint in genericParameter.GetGenericParameterConstraints()) {
               genericArguments = genericArguments.Where(constraint.IsAssignableFrom).ToArray();
            }
            if (genericParameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)) {
               genericArguments = genericArguments.Where(t => !t.IsValueType).ToArray();
            }
            genericArgumentsByParameterIndex[i] = genericArguments;
         }

         var testSets = combinatorialSetGenerator.GenerateCombinations(genericArgumentsByParameterIndex.Select(x => x.Length).ToArray());
         foreach (var testSet in testSets) {
            var genericArguments = new Type[testSet.Length];
            for (var i = 0; i < genericArguments.Length; i++) {
               genericArguments[i] = genericArgumentsByParameterIndex[i][testSet[i]];
            }

            var genericMethod = method.MakeGenericMethod(genericArguments);
            var genericStaticMethod = staticMethod.MakeGenericMethod(genericArguments);

            TestProxiedStaticMethod(staticField, genericMethod, genericStaticMethod);
         }

         //         var combinationCount = genericArgumentsByParameterIndex.Aggregate(1, (current, types) => current * types.Length);
         //         for (var i = 0; i < combinationCount; i++) {
         //            var genericArguments = new Type[genericParameters.Length];
         //            var counter = i;
         //            for (var genericParameterIndex = 0; genericParameterIndex < genericParameters.Length; genericParameterIndex++) {
         //               var genericArgumentCandidates = genericArgumentsByParameterIndex[genericParameterIndex];
         //               genericArguments[genericParameterIndex] = genericArgumentCandidates[counter % genericArgumentCandidates.Length];
         //               counter /= genericArgumentCandidates.Length;
         //            }
         //
         //            var genericMethod = method.MakeGenericMethod(genericArguments);
         //            var genericStaticMethod = staticMethod.MakeGenericMethod(genericArguments);
         //
         //            TestProxiedStaticMethod(staticField, genericMethod, genericStaticMethod);
         //         }
      }

      private void TestProxiedStaticMethod(FieldInfo staticField, MethodInfo method, MethodInfo staticMethod) {
         Console.WriteLine($"Invoking static method {staticMethod} which delegates to {staticField.FieldType}.");

         var interfaceType = staticField.FieldType;
         var mock = nmockito.CreateMock(interfaceType);
         staticField.SetValue(null, mock);

         var parameters = method.GetParameters();
         var originalArguments = new object[parameters.Length];
         var outResultsOrderedByIndex = new SortedDictionary<int, object>();
         for (var i = 0; i < parameters.Length; i++) {
            var parameter = parameters[i];
            if (parameter.IsOut) {
               var notByRefType = parameter.ParameterType.GetElementType();
               var outResult = nmockito.CreatePlaceholder(notByRefType);
               outResultsOrderedByIndex.Add(i, outResult);
               originalArguments[i] = notByRefType.GetDefaultValue();
            } else {
               originalArguments[i] = nmockito.CreatePlaceholder(parameter.ParameterType);
            }
         }

         var expectedReturnValue = method.ReturnType == typeof(void) ? null : nmockito.CreatePlaceholder(method.ReturnType);

         Console.WriteLine($"   Arguments: " + string.Join(", ", originalArguments));
         Console.WriteLine($"        Outs: " + (outResultsOrderedByIndex.Any() ? string.Join(", ", outResultsOrderedByIndex.Values) : "(none)"));
         Console.WriteLine($"     Returns: " + (expectedReturnValue?.ToString() ?? "null"));


         nmockito.Expect(() => method.Invoke(mock, originalArguments))
                 .SetOuts(outResultsOrderedByIndex.Values.ToArray())
                 .ThenReturn(expectedReturnValue);
         var invocationArguments = (object[])originalArguments.Clone();
         var actualReturnValue = staticMethod.Invoke(mock, invocationArguments);

         nmockito.AssertEquals(expectedReturnValue, actualReturnValue);
         foreach (var outResultAndIndex in outResultsOrderedByIndex) {
            nmockito.AssertEquals(outResultAndIndex.Value, invocationArguments[outResultAndIndex.Key]);
         }

         nmockito.VerifyExpectationsAndNoMoreInteractions();
         Console.WriteLine("");
      }
   }
}
