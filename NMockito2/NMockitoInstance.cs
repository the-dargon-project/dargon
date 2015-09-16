using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using NMockito2.Assertions;
using NMockito2.Expectations;
using NMockito2.Fluent;
using NMockito2.Mocks;
using NMockito2.Operations;
using NMockito2.Placeholders;
using NMockito2.SmartParameters;
using NMockito2.Transformations;
using NMockito2.Verification;

namespace NMockito2 {
   public class NMockitoInstance {
      public static NMockitoInstance Instance { get; set; }

      private readonly ProxyGenerator proxyGenerator;
      private readonly InvocationDescriptorFactory invocationDescriptorFactory;
      private readonly InvocationTransformer invocationTransformer;
      private readonly InvocationStage invocationStage;
      private readonly InvocationOperationManagerFinder invocationOperationManagerFinder;
      private readonly MockFactoryImpl mockFactory;
      private readonly SmartParameterStore smartParameterStore;
      private readonly SmartParameterPusher smartParameterPusher;
      private readonly AssertionsProxy assertionsProxy;
      private readonly ExpectationFactory expectationFactory;
      private readonly VerificationOperations verificationOperations;
      private readonly FluentExceptionAssertor fluentExceptionAssertor;
      private readonly VerificationOperationsProxy verificationOperationsProxy;
      private readonly PlaceholderFactory placeholderFactory;

      public NMockitoInstance() {
         Instance = this;

         proxyGenerator = new ProxyGenerator();
         invocationDescriptorFactory = new InvocationDescriptorFactory();
         IReadOnlyList<InvocationTransformation> transformations = new InvocationTransformation[] {
            new OutEnablingInvocationTransformationImpl(),
            new UnwrapParamsInvocationTransformationImpl(),
            new CreateImplicitEqualitySmartParametersInvocationTransformationImpl()
         };
         invocationTransformer = new InvocationTransformer(transformations);
         var verificationInvocationsContainer = new VerificationInvocationsContainer();
         invocationStage = new InvocationStage(verificationInvocationsContainer);
         invocationOperationManagerFinder = new InvocationOperationManagerFinder();
         mockFactory = new MockFactoryImpl(proxyGenerator, invocationDescriptorFactory, invocationTransformer, invocationStage, invocationOperationManagerFinder);
         smartParameterStore = new SmartParameterStore();
         smartParameterPusher = new SmartParameterPusher(smartParameterStore);
         assertionsProxy = new AssertionsProxy();
         expectationFactory = new ExpectationFactory(invocationStage, invocationOperationManagerFinder, verificationInvocationsContainer);
         verificationOperations = new VerificationOperations(invocationStage, verificationInvocationsContainer);
         ExceptionCaptorFactory exceptionCaptorFactory = new ExceptionCaptorFactory(proxyGenerator);
         fluentExceptionAssertor = new FluentExceptionAssertor(exceptionCaptorFactory);
         VerificationMockFactory verificationMockFactory = new VerificationMockFactory(proxyGenerator);
         verificationOperationsProxy = new VerificationOperationsProxy(invocationStage, verificationOperations, verificationMockFactory);
         placeholderFactory = new PlaceholderFactory(mockFactory);
      }

      public object CreateMock(Type type) => mockFactory.CreateMock(type);
      public T CreateMock<T>() where T : class => mockFactory.CreateMock<T>();
      public T CreateSpy<T>() where T : class => mockFactory.CreateSpy<T>();

      public object CreatePlaceholder(Type type) => placeholderFactory.CreatePlaceholder(type);
      public T CreatePlaceholder<T>() where T : class => placeholderFactory.CreatePlaceholder<T>();

      public T Any<T>() => Default<T>(smartParameterPusher.Any<T>);

      public Expectation When(Action action) => expectationFactory.Create(action, false);
      public Expectation<TResult> When<TResult>(TResult value) => expectationFactory.Create<TResult>(false);
      public Expectation<TOut1, TResult> When<TOut1, TResult>(Func<TOut1, TResult> func) => expectationFactory.Create<TOut1, TResult>(func, false);
      public Expectation<TOut1, TOut2, TResult> When<TOut1, TResult, TOut2>(Func<TOut1, TOut2, TResult> func) => expectationFactory.Create<TOut1, TOut2, TResult>(func, false);
      public Expectation<TOut1, TOut2, TOut3, TResult> When<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => expectationFactory.Create<TOut1, TOut2, TOut3, TResult>(func, false);

      public Expectation Expect(Action action) => expectationFactory.Create(action, true);
      public Expectation<TResult> Expect<TResult>(TResult func) => expectationFactory.Create<TResult>(true);
      public Expectation<TOut1, TResult> Expect<TOut1, TResult>(Func<TOut1, TResult> func) => expectationFactory.Create<TOut1, TResult>(func, true);
      public Expectation<TOut1, TOut2, TResult> Expect<TOut1, TOut2, TResult>(Func<TOut1, TOut2, TResult> func) => expectationFactory.Create<TOut1, TOut2, TResult>(func, true);
      public Expectation<TOut1, TOut2, TOut3, TResult> Expect<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => expectationFactory.Create<TOut1, TOut2, TOut3, TResult>(func, true);

      public void AssertEquals<T>(T expected, T actual) => assertionsProxy.AssertEquals(expected, actual);
      public void AssertTrue(bool value) => assertionsProxy.AssertTrue(value);
      public void AssertFalse(bool value) => assertionsProxy.AssertFalse(value);
      public void AssertNull(object value) => assertionsProxy.AssertNull(value);
      public void AssertNotNull(object value) => assertionsProxy.AssertNotNull(value);
      public void AssertThrows<TException>(Action action) where TException : Exception => assertionsProxy.AssertThrows<TException>(action);
      public AssertWithAction Assert(Action action) => assertionsProxy.AssertWithAction(action);
      
      // Fluent exception assertion:
      public TMock Assert<TMock>(TMock mock) where TMock : class => fluentExceptionAssertor.CreateExceptionCaptor(mock);
      internal void AssertThrown<TException>() where TException : Exception => fluentExceptionAssertor.AssertThrown<TException>();

      public void Verify(Action action) => verificationOperationsProxy.Verify(action);
      public TMock Verify<TMock>(TMock mock) where TMock : class => verificationOperationsProxy.Verify(mock);

      public void VerifyExpectations() => verificationOperations.VerifyExpectations();
      public void VerifyNoMoreInteractions() => verificationOperations.VerifyNoMoreInteractions();
      public void VerifyExpectationsAndNoMoreInteractions() => verificationOperations.VerifyExpectationsAndNoMoreInteractions();

      private T Default<T>(Action runThis) {
         runThis();
         return default(T);
      }

   }
}