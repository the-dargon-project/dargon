using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using NMockito2.Assertions;
using NMockito2.Attributes;
using NMockito2.Expectations;
using NMockito2.Fluent;
using NMockito2.Mocks;
using NMockito2.Operations;
using NMockito2.Placeholders;
using NMockito2.SmartParameters;
using NMockito2.Transformations;
using NMockito2.Verification;

namespace NMockito2 {
   public class NMockitoInstance : NMockitoCore {
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
      private readonly AttributesInitializer attributesInitializer;

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
         attributesInitializer = new AttributesInitializer(mockFactory);
         attributesInitializer.InitializeTestClassInstance(this);
      }

      public virtual object CreateMock(Type type) => mockFactory.CreateMock(type);
      public virtual T CreateMock<T>() where T : class => mockFactory.CreateMock<T>();
      public virtual T CreateSpy<T>() where T : class => mockFactory.CreateSpy<T>();

      public virtual object CreatePlaceholder(Type type) => placeholderFactory.CreatePlaceholder(type);
      public virtual T CreatePlaceholder<T>() => placeholderFactory.CreatePlaceholder<T>();

      public virtual T Any<T>() => Default<T>(smartParameterPusher.Any<T>);

      public virtual Expectation When(Action action) => expectationFactory.Create(action, false);
      public virtual Expectation<TResult> When<TResult>(TResult value) => expectationFactory.Create<TResult>(false);
      public virtual Expectation<TOut1, TResult> When<TOut1, TResult>(Func<TOut1, TResult> func) => expectationFactory.Create<TOut1, TResult>(func, false);
      public virtual Expectation<TOut1, TOut2, TResult> When<TOut1, TResult, TOut2>(Func<TOut1, TOut2, TResult> func) => expectationFactory.Create<TOut1, TOut2, TResult>(func, false);
      public virtual Expectation<TOut1, TOut2, TOut3, TResult> When<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => expectationFactory.Create<TOut1, TOut2, TOut3, TResult>(func, false);

      public virtual Expectation Expect(Action action) => expectationFactory.Create(action, true);
      public virtual Expectation<TResult> Expect<TResult>(TResult func) => expectationFactory.Create<TResult>(true);
      public virtual Expectation<TOut1, TResult> Expect<TOut1, TResult>(Func<TOut1, TResult> func) => expectationFactory.Create<TOut1, TResult>(func, true);
      public virtual Expectation<TOut1, TOut2, TResult> Expect<TOut1, TOut2, TResult>(Func<TOut1, TOut2, TResult> func) => expectationFactory.Create<TOut1, TOut2, TResult>(func, true);
      public virtual Expectation<TOut1, TOut2, TOut3, TResult> Expect<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => expectationFactory.Create<TOut1, TOut2, TOut3, TResult>(func, true);

      public virtual void AssertEquals<T>(T expected, T actual) => assertionsProxy.AssertEquals(expected, actual);
      public virtual void AssertTrue(bool value) => assertionsProxy.AssertTrue(value);
      public virtual void AssertFalse(bool value) => assertionsProxy.AssertFalse(value);
      public virtual void AssertNull(object value) => assertionsProxy.AssertNull(value);
      public virtual void AssertNotNull(object value) => assertionsProxy.AssertNotNull(value);
      public virtual void AssertThrows<TException>(Action action) where TException : Exception => assertionsProxy.AssertThrows<TException>(action);
      public virtual void AssertSequenceEquals<T>(IEnumerable<T> a, IEnumerable<T> b) => assertionsProxy.AssertSequenceEquals(a, b);
      public virtual AssertWithAction Assert(Action action) => assertionsProxy.AssertWithAction(action);
      
      // Fluent exception assertion:
      public virtual TMock Assert<TMock>(TMock mock) where TMock : class => fluentExceptionAssertor.CreateExceptionCaptor(mock);
      internal virtual void AssertThrown<TException>() where TException : Exception => fluentExceptionAssertor.AssertThrown<TException>();

      public virtual void Verify(Action action) => verificationOperationsProxy.Verify(action);
      public virtual TMock Verify<TMock>(TMock mock) where TMock : class => verificationOperationsProxy.Verify(mock);

      public virtual void VerifyExpectations() => verificationOperations.VerifyExpectations();
      public virtual void VerifyNoMoreInteractions() => verificationOperations.VerifyNoMoreInteractions();
      public virtual void VerifyExpectationsAndNoMoreInteractions() => verificationOperations.VerifyExpectationsAndNoMoreInteractions();

      private T Default<T>(Action runThis) {
         runThis();
         return default(T);
      }
   }
}