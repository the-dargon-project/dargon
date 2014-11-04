using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace NMockito
{
   public class NMockitoAnyTests : NMockitoInstance
   {
      private NMockitoAny<IDummyInterface> testObj;

      [Mock] private readonly IDummyCallback callback = null;
      [Mock] private readonly IDummyInterface dummyInterfaceImplementor = null;
      [Mock] private readonly IOtherInterface otherInterfaceImplementor = null;

      public NMockitoAnyTests() {
         testObj = new NMockitoAny<IDummyInterface>(callback.Test);
      }

      [Fact]
      public void NonmatchingInterfaceShortCircuitsTest()
      {
         AssertFalse(testObj.Test(otherInterfaceImplementor));
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void MatchingInterfaceRunsTest()
      {
         When(callback.Test(dummyInterfaceImplementor)).ThenReturn(true, false);
         
         AssertTrue(testObj.Test(dummyInterfaceImplementor));
         AssertFalse(testObj.Test(dummyInterfaceImplementor));
         
         Verify(callback, Times(2)).Test(dummyInterfaceImplementor);
         VerifyNoMoreInteractions();
      }

      public interface IDummyInterface
      {
      }

      public interface IOtherInterface 
      {
      }

      public interface IDummyCallback
      {
         bool Test(IDummyInterface value);
      }
   }
}
