using libtestutil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ItzWarty.Test.Test
{
   [TestClass]
   public class MockInitializationProcessorTest
   {
      private MockInitializationProcessor testObj = new MockInitializationProcessor();

      [TestMethod]
      public void MockInitializationProcessorInitializesUninitializedMock()
      {
         var mockContainer = new TestMockContainer();
         Assert.IsNull(mockContainer.GetDummyInterface());
         testObj.Process(mockContainer);
         Assert.IsNotNull(mockContainer.GetDummyInterface());
      }

      [TestMethod]
      public void MockInitializationProcessorDoesNotInitializeInitializedMock()
      {
         var mockContainer = new TestMockContainer2();
         var oldMock = mockContainer.GetDummyInterface();
         testObj.Process(mockContainer);
         Assert.AreEqual(oldMock, mockContainer.GetDummyInterface());
      }
   }

   public interface IDummyInterface
   {
   }

   public class TestMockContainer
   {
      private Mock<IDummyInterface> dummyInterface;

      public Mock<IDummyInterface> GetDummyInterface()
      {
         return dummyInterface;
      }
   }

   public class TestMockContainer2
   {
      private Mock<IDummyInterface> dummyInterface = new Mock<IDummyInterface>();

      public Mock<IDummyInterface> GetDummyInterface()
      {
         return dummyInterface;
      }
   }
}
