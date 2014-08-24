using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using libtestutil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language.Flow;

namespace ItzWarty.Test
{
   public class MockitoLike
   {
      private readonly Dictionary<object, object> mocksByObject = new Dictionary<object, object>();
      private readonly Dictionary<object, Type> interfaceByMock = new Dictionary<object, Type>();
      private readonly Dictionary<object, Type> interfaceByMockObject = new Dictionary<object, Type>();

      public void InitializeMocks()
      {
         MoqHelper.InitializeMocks(this);
         InitializeMockAttributeFields();
      }

      private void InitializeMockAttributeFields()
      {
         this.mocksByObject.Clear();

         var type = this.GetType();
         var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
         foreach (var field in fields) {
            var mockAttribute = field.GetCustomAttribute<MockAttribute>();
            if (mockAttribute != null) {
               var fieldType = field.FieldType;
               var mockType = typeof(Mock<>).MakeGenericType(fieldType);
               var mock = Activator.CreateInstance(mockType);
               //foreach (var property in mockType.GetProperties())
               //   Console.WriteLine(property.Name + " " + property.PropertyType);
               var mockObjectProperty = (from property in mockType.GetProperties()
                  where property.Name == "Object"
                  where property.PropertyType == fieldType
                  select property).First();
               var mockObject = mockObjectProperty.GetValue(mock);
               mocksByObject.Add(mockObject, mock);
               field.SetValue(this, mockObject);
               interfaceByMock.Add(mock, fieldType);
               interfaceByMockObject.Add(mockObject, fieldType);
            }
         }
      }

      public void verify(Expression<Action> expression, Times? times = null)
      {
         var lambda = (LambdaExpression)expression;
         var call = (MethodCallExpression)lambda.Body;
         var mockObject = Expression.Lambda<Func<object>>(call.Object).Compile()();
         var method = call.Method;
         var args = call.Arguments;

         var mock = this.GetMockFromObject(mockObject);
         var mockType = mock.GetType();

         var mockObjectInterface = this.GetInterfaceFromMock(mock);
         var helperDefinition = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First((m) => m.Name.StartsWith("VerifyHelper"));
         var helper = helperDefinition.MakeGenericMethod(mockObjectInterface);
         helper.Invoke(this, new object[] { mock, method, args, times == null ? Times.Once() : times.Value });
      }

      public void verify<T1>(Expression<Func<T1>> expression, Times? times = null)
      {
         var lambda = (LambdaExpression)expression;
         var call = (MethodCallExpression)lambda.Body;
         var mockObject = Expression.Lambda<Func<object>>(call.Object).Compile()();
         var method = call.Method;
         var args = call.Arguments;

         var mock = this.GetMockFromObject(mockObject);
         var mockType = mock.GetType();

         var mockObjectInterface = this.GetInterfaceFromMock(mock);
         var helperDefinition = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First((m) => m.Name.StartsWith("VerifyHelper"));
         var helper = helperDefinition.MakeGenericMethod(mockObjectInterface);
         helper.Invoke(this, new object[] { mock, method, args, times == null ? Times.Once() : times.Value });
      }

      internal void VerifyHelper<TMockInterface>(
         Mock<TMockInterface> mock,
         MethodInfo method,
         ReadOnlyCollection<Expression> arguments,
         Times times)
         where TMockInterface : class
      {
         mock.Verify(
            Expression.Lambda<Action<TMockInterface>>(
               Expression.Call(
                  Expression.Parameter(typeof(TMockInterface), "___param___"),
                  method,
                  arguments
                  ), Expression.Parameter(typeof(TMockInterface), "___param___")
               ), times
            );
      }

      internal Mock<T> GetMockFromObject<T>(T obj)
         where T : class { return (Mock<T>)mocksByObject[obj]; }

      internal Mock GetMockFromObject(object obj) { return (Mock)mocksByObject[obj]; }

      public WhenContext<TResult> when<TResult>(Expression<Func<TResult>> expression)
      {
         var lambda = (LambdaExpression)expression;
         var call = (MethodCallExpression)lambda.Body;
         return new WhenContext<TResult>(this, expression);
      }

      public Type GetInterfaceFromMock(Mock mock) { return this.interfaceByMock[mock]; }

      [DebuggerHidden]
      public void assertEquals<T>(T expected, T actual) { Assert.AreEqual(expected, actual); }

      [DebuggerHidden]
      public void assertNull<T>(T value) { Assert.IsNull(value); }

      [DebuggerHidden]
      public void assertNotNull<T>(T value) { Assert.IsNotNull(value); }

      [DebuggerHidden]
      public void assertTrue(bool value) { Assert.IsTrue(value); }

      [DebuggerHidden]
      public void assertFalse(bool value) { Assert.IsFalse(value); }
   }

   public class WhenContext<TResult>
   {
      private readonly MockitoLike mockitoLike;
      private readonly Expression<Func<TResult>> expression;

      public WhenContext(MockitoLike mockitoLike, Expression<Func<TResult>> expression)
      {
         this.mockitoLike = mockitoLike;
         this.expression = expression;
      }

      public void ThenReturn(TResult result)
      {
         var call = (MethodCallExpression)this.expression.Body;
         var mockObject = Expression.Lambda<Func<object>>(call.Object).Compile()();
         var method = call.Method;
         var args = call.Arguments;

         var mock = mockitoLike.GetMockFromObject(mockObject);
         var mockType = mock.GetType();
         var setupMethod = (from mockMethod in mockType.GetMethods()
                            where mockMethod.IsGenericMethod
                            where mockMethod.Name == "Setup"
                            select mockMethod).First();

         var mockObjectInterface = mockitoLike.GetInterfaceFromMock(mock);
         var helperDefinition = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First((m) => m.Name.StartsWith("ThenReturnHelper"));
         var helper = helperDefinition.MakeGenericMethod(mockObjectInterface, method.ReturnType);
         helper.Invoke(this, new object[] { mock, method, args, result });
      }

      private void ThenReturnHelper<TMockInterface, TResult>(
            Mock<TMockInterface> mock, 
            MethodInfo method, 
            ReadOnlyCollection<Expression> arguments, 
            TResult result)
         where TMockInterface : class
      {
         mock.Setup(
            Expression.Lambda<Func<TMockInterface, TResult>>(
               Expression.Call(
                  Expression.Parameter(typeof(TMockInterface), "___param___"),
                  method,
                  arguments
               ), Expression.Parameter(typeof(TMockInterface), "___param___")
            )
         ).Returns(result);
      }
   }
}
