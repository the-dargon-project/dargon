using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using NMockito2.Assertions;
using NMockito2.Expectations;
using NMockito2.Fluent;
using NMockito2.Mocks;
using NMockito2.Operations;
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
      private readonly InvocationOperationManagerConfigurer invocationOperationManagerConfigurer;
      private readonly AssertionsProxy assertionsProxy;
      private readonly ExpectationFactory expectationFactory;
      private readonly VerificationOperations verificationOperations;
      private readonly FluentExceptionAssertion fluentExceptionAssertion;

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
         invocationOperationManagerConfigurer = new InvocationOperationManagerConfigurer(invocationOperationManagerFinder, invocationStage);
         assertionsProxy = new AssertionsProxy();
         expectationFactory = new ExpectationFactory(invocationStage, invocationOperationManagerFinder, verificationInvocationsContainer);
         verificationOperations = new VerificationOperations(invocationStage, verificationInvocationsContainer);
         ExceptionCaptorFactory exceptionCaptorFactory = new ExceptionCaptorFactory(proxyGenerator);
         fluentExceptionAssertion = new FluentExceptionAssertion(exceptionCaptorFactory);
      }

      public T CreateMock<T>() where T : class => mockFactory.CreateMock<T>();

      public T Any<T>() => Default<T>(smartParameterPusher.Any<T>);

      public Expectation<TResult> When<TResult>(TResult value) => expectationFactory.When<TResult>();
      public Expectation<TOut1, TResult> When<TOut1, TResult>(Func<TOut1, TResult> func) => expectationFactory.When<TOut1, TResult>(func);
      public Expectation<TOut1, TOut2, TResult> When<TOut1, TResult, TOut2>(Func<TOut1, TOut2, TResult> func) => expectationFactory.When<TOut1, TOut2, TResult>(func);
      public Expectation<TOut1, TOut2, TOut3, TResult> When<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => expectationFactory.When<TOut1, TOut2, TOut3, TResult>(func);

      public Expectation<TResult> Expect<TResult>(TResult func) => expectationFactory.Expect<TResult>();
      public Expectation<TOut1, TResult> Expect<TOut1, TResult>(Func<TOut1, TResult> func) => expectationFactory.Expect<TResult, TOut1>(func);
      public Expectation<TOut1, TOut2, TResult> Expect<TOut1, TOut2, TResult>(Func<TOut1, TOut2, TResult> func) => expectationFactory.Expect<TOut1, TOut2, TResult>(func);
      public Expectation<TOut1, TOut2, TOut3, TResult> Expect<TOut1, TOut2, TOut3, TResult>(Func<TOut1, TOut2, TOut3, TResult> func) => expectationFactory.Expect<TOut1, TOut2, TOut3, TResult>(func);

      public Expectation Expect(Action action) => expectationFactory.Expect(action);

      public void AssertEquals<T>(T expected, T actual) => assertionsProxy.AssertEquals(expected, actual);
      public void AssertTrue(bool value) => assertionsProxy.AssertTrue(value);
      public void AssertFalse(bool value) => assertionsProxy.AssertFalse(value);
      public void AssertThrows<TException>(Action action) where TException : Exception => assertionsProxy.AssertThrows<TException>(action);

      // Exception assertion by action:
      public AssertWithAction Assert(Action action) => assertionsProxy.AssertWithAction(action);
      
      // Fluent exception assertion:
      public TMock Assert<TMock>(TMock mock) where TMock : class => fluentExceptionAssertion.CreateExceptionCaptor(mock);
      internal void SetLastException(Exception exception) => fluentExceptionAssertion.SetLastException(exception);
      internal void AssertThrown<TException>() where TException : Exception => fluentExceptionAssertion.AssertThrown<TException>();

      public void VerifyExpectations() => verificationOperations.VerifyExpectations();
      public void VerifyNoMoreInteractions() => verificationOperations.VerifyNoMoreInteractions();
      public void VerifyExpectationsAndNoMoreInteractions() => verificationOperations.VerifyExpectationsAndNoMoreInteractions();

      private T Default<T>(Action runThis) {
         runThis();
         return default(T);
      }

   }
}