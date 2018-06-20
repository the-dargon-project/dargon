using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace NMockito.Assertions {
   public class AssertionsProxy {
      // Where equals refers to .Equals invoke if not null.
      // xUnit does insane new int[0] == new int[0], which we support through
      // AssertSequenceEquals and AssertCollectionsDeepEquals.
      // We also support AssertReferenceEquals.
      public void AssertEquals<T>(T expected, T actual) {
         void Test(bool eq) {
            if (!eq) {
               throw new EqualException(expected, actual);
            }
         }

         if (expected == null && actual == null) Test(true);
         else if (expected != null) Test(expected.Equals(actual));
         else Test(actual.Equals(expected));
      }

      public void AssertNotEquals<T>(T expected, T actual) {
         void Test(bool eq) {
            if (eq) {
               // WTF xUnit takes string expected/actuals here?
               throw new NotEqualException(expected?.ToString() ?? "[null]", actual?.ToString() ?? "[null]");
            }
         }

         if (expected == null && actual == null) Test(true);
         else if (expected != null) Test(expected.Equals(actual));
         else Test(actual.Equals(expected));
      }

      public void AssertSequenceEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual) {
         Assert.Equal(expected, actual);
      }

      public void AssertCollectionsDeepEquals(IEnumerable a, IEnumerable b) {
         if (a == null || b == null) {
            AssertTrue(a == b);
         }

         // Assert a/b of same type, enumerable.
         AssertEquals(a.GetType(), b.GetType());

         var type = a.GetType();
         var typeInterfaces = type.GetInterfaces().ToArray();

         // Determine if is dictionary
         var isDictionaryLike = typeInterfaces.Any(t =>
            t == typeof(IDictionary) ||
            (t.GetTypeInfo().IsGenericType &&
             (t.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
              t.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
               ));

         // determine if order matters for dictionary
         var isUnorderedDictionaryLike = isDictionaryLike;
         if (isDictionaryLike) {
            var current = type;
            while (current != null && isUnorderedDictionaryLike) {
               if (current.GetTypeInfo().IsGenericType && current.GetGenericTypeDefinition() == typeof(SortedDictionary<,>)) {
                  isUnorderedDictionaryLike = false;
               }
               current = current.GetTypeInfo().BaseType;
            }
         }

         // determine if is a set
         var isSetLike = typeInterfaces.Any(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(ISet<>));

         var isUnorderedSetLike = isSetLike;
         if (isSetLike) {
            var current = type;
            while (current != null && isUnorderedSetLike) {
               if (current.GetTypeInfo().IsGenericType && current.GetGenericTypeDefinition() == typeof(SortedSet<>)) {
                  isUnorderedSetLike = false;
               }
               current = current.GetTypeInfo().BaseType;
            }
         }

         if (isUnorderedDictionaryLike) {
            dynamic aDynamic = a;
            dynamic bDynamic = b;
            var aKeys = new HashSet<object>(aDynamic.Keys);
            var bKeys = new HashSet<object>(bDynamic.Keys);
            AssertEquals(aKeys.Count, bKeys.Count);

            var aType = a.GetType();
            var indexer = aType.GetProperties().First(p => p.GetIndexParameters().Length == 1);

            foreach (var aKey in aKeys) {
               AssertTrue(bKeys.Contains(aKey));
               var aValue = indexer.GetValue(a, new[] { aKey });
               var bValue = indexer.GetValue(b, new[] { aKey });
               AssertCollectionDeepEquals_AssertElementEquals(aValue, bValue);
            }
         } else if (isUnorderedSetLike) {
            var aKeys = new HashSet<object>(a.Cast<object>());
            var bKeys = new HashSet<object>(b.Cast<object>());
            AssertEquals(aKeys.Count, bKeys.Count);
            foreach (var aKey in aKeys) {
               AssertTrue(bKeys.Contains(aKey));
            }
         } else {
            var ita = ((IEnumerable)a).GetEnumerator();
            var itb = ((IEnumerable)b).GetEnumerator();

            var hasCurrent = true;
            while (hasCurrent) {
               hasCurrent = ita.MoveNext();
               AssertEquals(hasCurrent, itb.MoveNext());

               if (hasCurrent) {
                  AssertCollectionDeepEquals_AssertElementEquals(ita.Current, itb.Current);
               }
            }
         }
      }

      public void AssertCollectionDeepEquals_AssertElementEquals(object a, object b) {
         if (a == null || b == null) {
            AssertTrue(a == b);
         }

         var aType = a.GetType();
         AssertEquals(aType, b.GetType());

         if (a is IEnumerable) {
            AssertCollectionsDeepEquals((IEnumerable)a, (IEnumerable)b);
         } else if (aType.GetTypeInfo().IsGenericType && aType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
            dynamic aKvp = a;
            dynamic bKvp = b;
            AssertCollectionDeepEquals_AssertElementEquals(aKvp.Key, bKvp.Key);
            AssertCollectionDeepEquals_AssertElementEquals(aKvp.Value, bKvp.Value);
         } else {
            AssertEquals(a, b);
         }
      }

      public void AssertTrue(bool value) => Assert.True(value);
      public void AssertFalse(bool value) => Assert.False(value);
      public void AssertNull(object obj) => Assert.Null(obj);
      public void AssertNotNull(object obj) => Assert.NotNull(obj);

      public void AssertThrows<TException>(Action action) where TException : Exception {
         try {
            action();
         } catch (Exception e) {
            if (e.GetType() != typeof(TException)) {
               throw new IncorrectOuterThrowException(typeof(TException), e);
            }
            return;
         }
         throw new NothingThrownException(typeof(TException));
      }

      public void AssertThrows<TOuterException, TInnerException>(Action action) where TOuterException : Exception where TInnerException : Exception {
         try {
            action();
         } catch (Exception e) {
            if (e.GetType() != typeof(TOuterException)) {
               throw new IncorrectOuterThrowException(typeof(TOuterException), e);
            } else if (!TestForInnerException<TInnerException>(e)) {
               throw new IncorrectInnerThrowException(typeof(TInnerException), e);
            }
            return;
         }
         throw new NothingThrownException(typeof(TOuterException), typeof(TInnerException));
      }

      private bool TestForInnerException<TInnerException>(Exception exception) where TInnerException : Exception {
         switch (exception) {
            case TInnerException _:
            case AggregateException ae when ae.InnerExceptions.Any(TestForInnerException<TInnerException>):
            case Exception _ when exception.InnerException != null &&
                                  TestForInnerException<TInnerException>(exception.InnerException):
               return true;
            default:
               return false;
         }
      }

      public AssertWithAction AssertWithAction(Action action) => new AssertWithAction(this, action);
   }
}
