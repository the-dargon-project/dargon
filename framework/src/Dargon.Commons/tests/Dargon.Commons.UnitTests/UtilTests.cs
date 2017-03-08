using NMockito;
using System;
using Xunit;

namespace Dargon.Commons {
   public partial class UtilTests : NMockitoInstance {
      private static readonly byte[] buffer = Arrays.Create(255, i => (byte)i);
      private readonly byte[] bufferCopy = Arrays.Create(buffer.Length, i => buffer[i]);

      [Fact]
      public void ByteArraysEqual_HappyPathTest() {
         for (var size = 0; size < 255; size++) {
            for (var i = 0; i < buffer.Length - size + 1; i++) {
               for (var j = 0; j < bufferCopy.Length - size + 1; j++) {
                  AssertEquals(i == j || size == 0, Bytes.ArraysEqual(buffer, i, size, bufferCopy, j, size));
               }
            }
         }
      }

      [Fact]
      public void ByteArraysEqual_TrivialHappyPathTest() {
         AssertTrue(Bytes.ArraysEqual(buffer, buffer));
      }

      [Fact]
      public void ByteArraysEqual_TrivialHappyPathWithOffsetTest() {
         AssertTrue(Bytes.ArraysEqual(buffer, 0, buffer, 0, buffer.Length));
      }

      [Fact]
      public void ByteArraysEqual_TrivialSadPathTest() {
         AssertFalse(Bytes.ArraysEqual(new byte[0], new byte[1]));
      }

      [Fact]
      public void ByteArraysEqual_BoundsTest() {
         byte[] dummyBuffer = new byte[1];
         AssertThrows<IndexOutOfRangeException>(() => Bytes.ArraysEqual(buffer, 1, buffer.Length, dummyBuffer, 0, 1));
         AssertThrows<IndexOutOfRangeException>(() => Bytes.ArraysEqual(dummyBuffer, 0, 1, buffer, 1, buffer.Length));

         AssertThrows<IndexOutOfRangeException>(() => Bytes.ArraysEqual(buffer, -1, 1, dummyBuffer, 0, 1));
         AssertThrows<IndexOutOfRangeException>(() => Bytes.ArraysEqual(dummyBuffer, 0, 1, buffer, -1, 1));

         AssertThrows<IndexOutOfRangeException>(() => Bytes.ArraysEqual(buffer, 1, -1, dummyBuffer, 0, 1));
         AssertThrows<IndexOutOfRangeException>(() => Bytes.ArraysEqual(dummyBuffer, 0, 1, buffer, 1, -1));
      }

      [Fact]
      public void NextToken_SimpleTest() {
         string str = "asdf qwerty  yuiop";
         string token;

         str = Tokenizer.Next(str, out token);
         AssertEquals("asdf", token);
         AssertEquals("qwerty  yuiop", str);

         str = Tokenizer.Next(str, out token);
         AssertEquals("qwerty", token);
         AssertEquals(" yuiop", str);

         str = Tokenizer.Next(str, out token);
         AssertEquals("yuiop", token);
         AssertEquals("", str);
      }

      [Fact]
      public void ToTitleCaseTests() {
         AssertEquals("", "".ToTitleCase());
         AssertEquals("", "!@#)!#@*&#".ToTitleCase());
         AssertEquals("This Is A Test", "This is a test".ToTitleCase());
         AssertEquals("Tencent Art Pack", "tencent-art-pack".ToTitleCase());
         AssertEquals("Tencent Art Pack", "tencent&#@*&!art-pack".ToTitleCase());
      }
   }
}
