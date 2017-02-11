using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito;
using Xunit;

namespace Dargon.Ryu {
   public class RyuConstructorTests : NMockitoInstance {
      private readonly IRyuFacade ryu;

      public RyuConstructorTests() {
         ryu = new RyuFactory().Create();
      }

      [Fact]
      public void Construct_AmbiguousConstructors_ThrowsExceptionTest() {
         AssertThrows<RyuActivateException, MultipleConstructorsFoundException>(() => ryu.Activate<ClassA>());
      }

      [Fact]
      public void Construct_ParameterlessRyuCtor_HappyPathTest() {
         ryu.Activate<ClassB>();
      }

      [Fact]
      public void Construct_ParameterfulRyuCtor_HappyPathTest() {
         ryu.Set(CreateMock<Dependency>());
         ryu.Activate<ClassC>();
      }

      [Fact]
      public void Construct_ParameterfulRyuCtor_SadPathTest() {
         AssertThrows<RyuActivateException, ImplementationNotFoundException>(() => ryu.Activate<ClassC>());
      }

      [Fact]
      public void Construct_NoCtor_SadPathTest() {
         AssertThrows<RyuActivateException, NoConstructorsFoundException>(() => ryu.Activate<ClassD>());
      }

      [Fact]
      public void Construct_SingleCtor_SadPathTest() {
         ryu.Activate<ClassE>();
      }
   }

   public interface Dependency { }

   public class ClassA {
      public ClassA() { }
      public ClassA(Dependency A) { }
   }

   public class ClassB {
      [RyuConstructor]
      public ClassB() { }
      public ClassB(Dependency A) { throw new InvalidOperationException(); }
   }

   public class ClassC {
      public ClassC() { throw new InvalidOperationException(); }
      [RyuConstructor]
      public ClassC(Dependency A) { }
   }

   public class ClassD {
      private ClassD() { }
   }

   public class ClassE {
      public ClassE() { }
   }
}
