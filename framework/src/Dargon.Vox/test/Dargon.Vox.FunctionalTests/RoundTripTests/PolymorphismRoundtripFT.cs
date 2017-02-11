using System;
using System.IO;
using Dargon.Vox.RoundTripTests;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class PolymorphismRoundtripFT : RoundTripTest {
      private static readonly Type[] kTestTypes = {
         typeof(Interface),
         typeof(Implementor),
         typeof(Subclass)
      };

      public PolymorphismRoundtripFT() : base(kTestTypes) { }

      public void Run() {
         RunTestHelper<Subclass, Interface, Subclass>(new Subclass());
         RunTestHelper<Subclass, Implementor, Subclass>(new Subclass());
         RunTestHelper<Subclass, Subclass, Subclass>(new Subclass());
         RunTestHelper<Implementor, Interface, Implementor>(new Implementor());
         RunTestHelper<Implementor, Implementor, Implementor>(new Implementor());
      }

      private void RunTestHelper<TInput, TRequestedOutput, TActualOutput>(TInput input) {
         using (var ms = new MemoryStream()) {
            serializer.Serialize(ms, input);
            ms.Position = 0;
            var output = serializer.Deserialize(ms, typeof(TRequestedOutput));
            AssertTrue(output.GetType() == typeof(TActualOutput));
         }
      }

      public interface Interface { }

      [AutoSerializable]
      public class Implementor : Interface {
         public string A { get; set; }
      }

      [AutoSerializable]
      public class Subclass : Implementor {
         public string B { get; set; }
      }
   }
}
