using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Messages.Data;

namespace Dargon.Messages {
   public class Program {
      public static void Main() {
         new VariableIntegerSerializationStatics_AcceptanceTests().Numbers_From_2Pow6_To_TwoPow13Min1_Take_Two_Bytes();
         new VariableIntegerSerializationStatics_AcceptanceTests().Numbers_From_Neg2Pow6_To_2Pow6Min1_Take_One_Byte();
         new VariableIntegerSerializationStatics_AcceptanceTests().Numbers_From_NegTwoPow13_To_Neg2Pow6Min1_Take_Two_Bytes();
         new VariableIntegerSerializationStatics_AcceptanceTests().Serialized_Zero_Is_Zero_Byte();

         new VariableIntegerSerializationStatics_RoundTripTests().EdgeCasesInts();
      }
   }
}
