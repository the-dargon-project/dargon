using System;
using System.Collections.Generic;

namespace Dargon.Vox.InternalTestUtils.Debugging {
   public class NullSlotReader : ISlotReader {
      public object ReadObject(int slot) => null;
      public T ReadObject<T>(int slot) => default(T);
      public byte[] ReadBytes(int slot) => null;
      public bool ReadBoolean(int slot) => false;
      public int ReadNumeric(int slot) => 0;
      public string ReadString(int slot) => null;
      public Type ReadType(int slot) => null;
      public DateTime ReadDateTime(int slot) => default(DateTime);
      public float ReadFloat(int slot) => default(float);
      public double ReadDouble(int slot) => default(double);

      public Guid ReadGuid(int slot) => Guid.Empty;
      public object ReadNull(int slot) => null;
      public TCollection ReadCollection<TElement, TCollection>(int slot) where TCollection : IEnumerable<TElement> {
         return default(TCollection);
      }
   }
}
