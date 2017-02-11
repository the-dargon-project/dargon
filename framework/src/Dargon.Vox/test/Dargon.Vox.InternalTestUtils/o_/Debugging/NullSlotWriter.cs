using System;
using System.Collections.Generic;

namespace Dargon.Vox.InternalTestUtils.Debugging {
   public class NullSlotWriter : ISlotWriter {
      public void WriteObject<T>(int slot, T x) {}
      public void WriteBytes(int slot, byte[] bytes) {}
      public void WriteBytes(int slot, byte[] bytes, int offset, int length) {}
      public void WriteBoolean(int slot, bool val) {} 
      public void WriteNumeric(int slot, int val) {}
      public void WriteString(int slot, string s) {}
      public void WriteType(int slot, Type type) {}
      public void WriteDateTime(int slot, DateTime dateTime) {}
      public void WriteFloat(int slot, float value) {}
      public void WriteDouble(int slot, double value) {}

      public void WriteGuid(int slot, Guid guid) {}
      public void WriteNull(int slot) {}
      public void WriteCollection<TElement, TCollection>(int slot, TCollection collection) where TCollection : IEnumerable<TElement> {}
   }
}