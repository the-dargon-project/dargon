using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NMockito.Fluent;
using Xunit;
using Xunit.Sdk;

namespace NMockito.UnitTests.Assertions {
   public class FluentAssertionStaticsTests : NMockitoInstance {
      private readonly object kObjectA = new object();
      private readonly object kObjectB = new object();

      [Fact] public void Int8_HappyPath() => ((sbyte)1).IsEqualTo(1);
      [Fact] public void Int8_SadPath() => AssertThrows<EqualException>(() => ((sbyte)1).IsEqualTo(2));

      [Fact] public void UInt8_HappyPath() => ((byte)1).IsEqualTo(1);
      [Fact] public void UInt8_SadPath() => AssertThrows<EqualException>(() => ((byte)1).IsEqualTo(2));

      [Fact] public void Int16_HappyPath() => ((short)1).IsEqualTo(1);
      [Fact] public void Int16_SadPath() => AssertThrows<EqualException>(() => ((short)1).IsEqualTo(2));
      
      [Fact] public void UInt16_HappyPath() => ((ushort)1).IsEqualTo(1);
      [Fact] public void UInt16_SadPath() => AssertThrows<EqualException>(() => ((ushort)1).IsEqualTo(2));
      
      [Fact] public void Object_HappyPath() => kObjectA.IsEqualTo(kObjectA);
      [Fact] public void Object_SadPath() => AssertThrows<EqualException>(() => kObjectA.IsEqualTo(kObjectB));
      
      [Fact] public void True_HappyPath() => true.IsTrue();
      [Fact] public void True_SadPath() => AssertThrows<TrueException>(() => false.IsTrue());
      
      [Fact] public void False_HappyPath() => false.IsFalse();
      [Fact] public void False_SadPath() => AssertThrows<FalseException>(() => true.IsFalse());
   }
}
