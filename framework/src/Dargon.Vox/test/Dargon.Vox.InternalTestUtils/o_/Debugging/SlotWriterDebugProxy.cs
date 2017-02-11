using System;
using System.Collections.Generic;

namespace Dargon.Vox.InternalTestUtils.Debugging {
   public class SlotWriterDebugProxy : ISlotWriter {
      private readonly ISlotWriter target;
      private int indentation = 0;

      public SlotWriterDebugProxy(ISlotWriter target = null) {
         this.target = target ?? new NullSlotWriter();
      }

      public void WriteObject<T>(int slot, T x) {
         Out(slot, "Write Object: " + (x?.ToString() ?? "[Null]"));
         indentation++;
         target.WriteObject(slot, x);
         indentation--;
      }

      public void WriteBytes(int slot, byte[] bytes) {
         Out(slot, "Write Bytes: Length " + bytes.Length);
         target.WriteBytes(slot, bytes);
      }

      public void WriteBytes(int slot, byte[] bytes, int offset, int length) {
         Out(slot, "Write Bytes: Length " + bytes.Length + " Offset " + offset);
         target.WriteBytes(slot, bytes, offset, length);
      }

      public void WriteBoolean(int slot, bool val) {
         Out(slot, "Write Boolean: " + val);
         target.WriteBoolean(slot, val);
      }

      public void WriteNumeric(int slot, int val) {
         Out(slot, "Write Numeric: " + val);
         target.WriteNumeric(slot, val);
      }

      public void WriteGuid(int slot, Guid guid) {
         Out(slot, "Write Guid: " + guid);
         target.WriteGuid(slot, guid);
      }

      public void WriteString(int slot, string s) {
         Out(slot, "Write String: " + s);
         target.WriteString(slot, s);
      }

      public void WriteType(int slot, Type type) {
         Out(slot, "Write Type: " + type);
         target.WriteType(slot, type);
      }

      public void WriteDateTime(int slot, DateTime dateTime) {
         Out(slot, "Write DateTime: " + dateTime);
         target.WriteDateTime(slot, dateTime);
      }

      public void WriteFloat(int slot, float value) {
         Out(slot, "Write Float: " + value);
         target.WriteFloat(slot, value);
      }

      public void WriteDouble(int slot, double value) {
         Out(slot, "Write Double: " + value);
         target.WriteDouble(slot, value);
      }

      public void WriteNull(int slot) {
         Out(slot, "Write Null");
         target.WriteNull(slot);
      }

      public void WriteCollection<TElement, TCollection>(int slot, TCollection collection) where TCollection : IEnumerable<TElement> {
         Out(slot, "Write Collection");
         target.WriteCollection<TElement, TCollection>(slot, collection);
      }

      private void Out(int slot, string desc) {
         Console.WriteLine(new string('\t', indentation) + slot + " => " + desc);
      }
   }
}