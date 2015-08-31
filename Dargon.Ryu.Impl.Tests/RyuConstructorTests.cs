using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMockito;
using Xunit;

namespace Dargon.Ryu {
   public class RyuConstructorTests : NMockitoInstance {
      private readonly RyuContainer ryu;

      public RyuConstructorTests() {
         ryu = new RyuFactory().Create();
      }

      [Fact]
      public void Construct_AmbiguousConstructors_ThrowsExceptionTest() {
         AssertThrows<MultipleConstructorsFoundException>(() => ryu.ForceConstruct<ClassA>());
      }

      [Fact]
      public void Construct_ParameterlessRyuCtor_HappyPathTest() {
         ryu.ForceConstruct<ClassB>();
      }

      [Fact]
      public void Construct_ParameterfulRyuCtor_HappyPathTest() {
         ryu.Set(CreateMock<Dependency>());
         ryu.ForceConstruct<ClassC>();
      }

      [Fact]
      public void Construct_ParameterfulRyuCtor_SadPathTest() {
         AssertThrows<RyuGetException>(() => ryu.ForceConstruct<ClassC>());
      }

      [Fact]
      public void Construct_NoCtor_SadPathTest() {
         AssertThrows<NoConstructorsFoundException>(() => ryu.ForceConstruct<ClassD>());
      }

      [Fact]
      public void Construct_SingleCtor_SadPathTest() {
         ryu.ForceConstruct<ClassE>();
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
