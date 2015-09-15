using System;
using NMockito2.Mocks;
using NMockito2.Operations;

namespace NMockito2.Expectations {
   public class Expectation {
      private readonly InvocationDescriptor invocationDescriptor;
      private readonly InvocationOperationManagerFinder invocationOperationManagerFinder;

      public Expectation(InvocationDescriptor invocationDescriptor, InvocationOperationManagerFinder invocationOperationManagerFinder) {
         this.invocationDescriptor = invocationDescriptor;
         this.invocationOperationManagerFinder = invocationOperationManagerFinder;
      }

      public Expectation ThenThrow(params Exception[] exceptions) {
         foreach (var exception in exceptions) {
            invocationOperationManagerFinder.AddInvocationOperation(
               invocationDescriptor,
               new ThrowInvocationOperation(exception));
         }
         return this;
      }
   }

   public class Expectation<TReturnValue> {
      private readonly InvocationDescriptor invocationDescriptor;
      private readonly InvocationOperationManagerFinder invocationOperationManagerFinder;

      public Expectation(InvocationDescriptor invocationDescriptor, InvocationOperationManagerFinder invocationOperationManagerFinder) {
         this.invocationDescriptor = invocationDescriptor;
         this.invocationOperationManagerFinder = invocationOperationManagerFinder;
      }

      public Expectation<TReturnValue> ThenReturn(params TReturnValue[] values) {
         if (values == null) {
            values = new TReturnValue[1];
         }

         foreach (var value in values) {
            invocationOperationManagerFinder.AddInvocationOperation(
               invocationDescriptor,
               new ReturnInvocationOperation(value));
         }
         return this;
      }

      public Expectation<TReturnValue> ThenThrow(params Exception[] exceptions) {
         foreach (var exception in exceptions) {
            invocationOperationManagerFinder.AddInvocationOperation(
               invocationDescriptor,
               new ThrowInvocationOperation(exception));
         }
         return this;
      }
   }

   public class Expectation<TOut1, TReturnValue> {
      private readonly InvocationDescriptor invocationDescriptor;
      private readonly InvocationOperationManagerFinder invocationOperationManagerFinder;

      public Expectation(InvocationDescriptor invocationDescriptor, InvocationOperationManagerFinder invocationOperationManagerFinder) {
         this.invocationDescriptor = invocationDescriptor;
         this.invocationOperationManagerFinder = invocationOperationManagerFinder;
      }

      public Expectation<TOut1, TReturnValue> SetOut(TOut1 out1) {
         invocationOperationManagerFinder.AddInvocationOperation(
            invocationDescriptor,
            new SetOutsInvocationOperation(out1));
         return this;
      }

      public Expectation<TOut1, TReturnValue> ThenReturn(TReturnValue value) {
         invocationOperationManagerFinder.AddInvocationOperation(
            invocationDescriptor,
            new ReturnInvocationOperation(value));
         return this;
      }

      public Expectation<TOut1, TReturnValue> ThenThrow(params Exception[] exceptions) {
         foreach (var exception in exceptions) {
            invocationOperationManagerFinder.AddInvocationOperation(
               invocationDescriptor,
               new ThrowInvocationOperation(exception));
         }
         return this;
      }
   }

   public class Expectation<TOut1, TOut2, TReturnValue> {
      private readonly InvocationDescriptor invocationDescriptor;
      private readonly InvocationOperationManagerFinder invocationOperationManagerFinder;

      public Expectation(InvocationDescriptor invocationDescriptor, InvocationOperationManagerFinder invocationOperationManagerFinder) {
         this.invocationDescriptor = invocationDescriptor;
         this.invocationOperationManagerFinder = invocationOperationManagerFinder;
      }

      public Expectation<TOut1, TOut2, TReturnValue> SetOut(TOut1 out1, TOut2 out2) {
         invocationOperationManagerFinder.AddInvocationOperation(
            invocationDescriptor,
            new SetOutsInvocationOperation(out1, out2));
         return this;
      }

      public Expectation<TOut1, TOut2, TReturnValue> ThenReturn(TReturnValue value) {
         invocationOperationManagerFinder.AddInvocationOperation(
            invocationDescriptor,
            new ReturnInvocationOperation(value));
         return this;
      }

      public Expectation<TOut1, TOut2, TReturnValue> ThenThrow(params Exception[] exceptions) {
         foreach (var exception in exceptions) {
            invocationOperationManagerFinder.AddInvocationOperation(
               invocationDescriptor,
               new ThrowInvocationOperation(exception));
         }
         return this;
      }
   }

   public class Expectation<TOut1, TOut2, TOut3, TReturnValue> {
      private readonly InvocationDescriptor invocationDescriptor;
      private readonly InvocationOperationManagerFinder invocationOperationManagerFinder;

      public Expectation(InvocationDescriptor invocationDescriptor, InvocationOperationManagerFinder invocationOperationManagerFinder) {
         this.invocationDescriptor = invocationDescriptor;
         this.invocationOperationManagerFinder = invocationOperationManagerFinder;
      }

      public Expectation<TOut1, TOut2, TOut3, TReturnValue> SetOut(TOut1 out1, TOut2 out2, TOut3 out3) {
         invocationOperationManagerFinder.AddInvocationOperation(
            invocationDescriptor,
            new SetOutsInvocationOperation(out1, out2, out3));
         return this;
      }

      public Expectation<TOut1, TOut2, TOut3, TReturnValue> ThenReturn(TReturnValue value) {
         invocationOperationManagerFinder.AddInvocationOperation(
            invocationDescriptor,
            new ReturnInvocationOperation(value));
         return this;
      }

      public Expectation<TOut1, TOut2, TOut3, TReturnValue> ThenThrow(params Exception[] exceptions) {
         foreach (var exception in exceptions) {
            invocationOperationManagerFinder.AddInvocationOperation(
               invocationDescriptor,
               new ThrowInvocationOperation(exception));
         }
         return this;
      }
   }
}
