using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NMockito;
using Xunit;

namespace Dargon.Vox.FunctionalTests.RoundTripTests {
   public class AutoTypeSerializerRoundTripFT : NMockitoInstance {
      [Fact]
      public void SerializeTest() {
         var writer = CreateMock<IBodyWriter>();
         Expect(() => writer.Write(10));
         Expect(() => writer.Write("Devexer"));

         var serializer = AutoTypeSerializerFactory.Create<Box<int>>();
         var target = new Box<int> { TValue = 10, String = "Devexer" };
         serializer.Serialize(writer, target);

         VerifyExpectationsAndNoMoreInteractions();
      }

      [Fact]
      public void DeserializeTest() {
         var reader = CreateMock<IBodyReader>();
         Expect(reader.Read<int>()).ThenReturn(10);
         Expect(reader.Read<string>()).ThenReturn("Devexer");

         var serializer = AutoTypeSerializerFactory.Create<Box<int>>();
         var target = new Box<int>();
         serializer.Deserialize(reader, target);

         AssertEquals(10, target.TValue);
         AssertEquals("Devexer", target.String);

         VerifyExpectationsAndNoMoreInteractions();
      }

      public class Box<T> {
         public T TValue { get; set; }
         public string String { get; set; }
      }
   }
}
