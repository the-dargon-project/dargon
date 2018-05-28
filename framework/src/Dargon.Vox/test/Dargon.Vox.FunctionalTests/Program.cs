using System;
using System.Diagnostics;
using System.IO;
using Dargon.Vox.FunctionalTests.RoundTripTests;
using Dargon.Vox.RoundTripTests;

namespace Dargon.Vox {
   public class Program {
      public static void Main() {
         var cout = Console.Out;
         Console.SetOut(TextWriter.Null);
         var sw = new Stopwatch();
         sw.Start();
         for (var i = 0; i < 1; i++) {
            new ByteArraySliceRoundTripFT().Run();

            new StringRoundTripFT().StringRoundTripTest();
            new StringRoundTripFT().StringArrayRoundTripTest();

            new EnumRoundtripFT().EnumValuesTest();
            new EnumRoundtripFT().EnumArraysTest();
            new IntRoundtripFT().EdgeCaseTest();
            new GuidRoundTripFT().Run();

            new BooleanRoundtripFT().TrueRoundTripTest();
            new BooleanRoundtripFT().FalseRoundTripTest();
            new BooleanRoundtripFT().ThisIsMaybeProbablyTotallyGoingToBreaktest();

            new TypeRoundTripFT().TypeRoundTripTest();
            new TypeRoundTripFT().TypeArrayRoundTripTest();
            new TypeRoundTripFT().TypeArrayArrayRoundTripTest();
            new TypeRoundTripFT().TypeSimplificationTest();

            new FloatingPointRoundtripFT().SingleRoundTripTest();
            new FloatingPointRoundtripFT().DoubleRoundTripTest();

            new DateTimeRoundTripFT().Run();
            new TimeSpanRoundTripFT().Run();

            new ObjectRoundTripFT().Run();

            new VectorLikeRoundtripFT().ArrayEasyTest();
            new VectorLikeRoundtripFT().ArrayHardTest1();
            new VectorLikeRoundtripFT().ArrayHardTest2();
            new VectorLikeRoundtripFT().ListEasyTest();
            new VectorLikeRoundtripFT().ListHardTest1();
            new VectorLikeRoundtripFT().ListHardTest2();

            new DictionaryLikeRoundtripFT().EasyTest();
            new DictionaryLikeRoundtripFT().HardTest1();
            new DictionaryLikeRoundtripFT().HardTest2();
            new DictionaryLikeRoundtripFT().HardTest3();

            new AutoTypeSerializerRoundTripFT().SerializeTest();
            new AutoTypeSerializerRoundTripFT().DeserializeTest();

            new AutoSerializableRoundTripTest1().EasyTest();
            new AutoSerializableRoundTripTest1().HardTest1();
            new AutoSerializableRoundTripTest1().HardTest2();

            new AutoSerializableRoundTripTest2().Hodgepodge();
            new AutoSerializableRoundTripTest2().LinkedList_String_Chain();

            new PolymorphismRoundtripFT().Run();

            new GlobalSerializerTests().Run();
         }
         Console.SetOut(cout);
         Console.WriteLine(sw.ElapsedMilliseconds);

         //         new BytesIT().ByteArrays();
         //         new BoolIT().True();
         //         new BoolIT().False();
         //         new GuidIT().Guids();
         //         new AutoserializationIT().LinkedList_String_SingleNode();
         //         new AutoserializationIT().LinkedList_String_Chain();
         //         new AutoserializationIT().Hodgepodge();
      }
   }
}
