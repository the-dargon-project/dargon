using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Vox;
using NMockito;
using NMockito.Attributes;
using Xunit;

namespace Dargon.Messages {
   public class InlineTypeSerializerTests : NMockitoInstance {
      [Mock] private readonly InvocationTarget target = null;
      private readonly InlineTypeSerializer<ISubject> testObj;

      public InlineTypeSerializerTests() {
         testObj = new InlineTypeSerializer<ISubject>(target.Write, target.Read);
      }

      [Fact]
      public void Type_ReturnsGenericParameter() {
         AssertEquals(typeof(ISubject), testObj.Type);
      }

      [Fact]
      public void Serialize_DelegatesTo_WriteConstructorParameter() {
         var writer = CreateMock<ISlotWriter>();
         var subject = CreateMock<ISubject>();

         testObj.Serialize(writer, subject);

         Verify(target).Write(writer, subject);
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void Deserialize_DelegatesTo_ReadConstructorParameter() {
         var reader = CreateMock<ISlotReader>();
         var subject = CreateMock<ISubject>();

         testObj.Deserialize(reader, subject);

         Verify(target).Read(reader, subject);
         VerifyNoMoreInteractions();
      }

      public interface ISubject { }

      public interface InvocationTarget {
         void Write(ISlotWriter writer, ISubject subject);
         void Read(ISlotReader reader, ISubject subject);
      }
   }
}
