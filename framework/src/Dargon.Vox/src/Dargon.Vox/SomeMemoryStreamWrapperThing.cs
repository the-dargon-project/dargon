using System;
using System.Collections.Generic;
using System.IO;

namespace Dargon.Vox {
   public class SomeMemoryStreamWrapperThing {
      public struct FredTheBanana {
         public int Length;
         public int Skip;
      }

      private const int kMaxLengthByteCount = 5;
      private readonly List<FredTheBanana> bananas = new List<FredTheBanana>();
      private readonly Action<int> addToByteCountFunc;
      private readonly Func<int> getByteCountFunc;
      private int byteCount = 0;

      public SomeMemoryStreamWrapperThing(MemoryStream target) {
         Target = target;
         this.addToByteCountFunc = c => byteCount += c;
         this.getByteCountFunc = () => byteCount;
      }

      public MemoryStream Target { get; }
      public IReadOnlyList<FredTheBanana> Bananas => bananas;

      public IDisposable ReserveLength() {
         var offset = Target.Position;
         Target.Position += kMaxLengthByteCount;
         var fredTheBanana = new FredTheBanana { Length = -1, Skip = -1 };
         var fredIndex = bananas.Count;
         bananas.Add(fredTheBanana);
         return new LengthReservation(offset, Target, bananas, fredIndex, addToByteCountFunc, getByteCountFunc);
      }

      public CountReservation ReserveCount() {
         // todo: code duplication
         var offset = Target.Position;
         Target.Position += kMaxLengthByteCount;
         var fredTheBanana = new FredTheBanana { Length = -1, Skip = -1 };
         var fredIndex = bananas.Count;
         bananas.Add(fredTheBanana);
         return new CountReservation(offset, Target, bananas, fredIndex, addToByteCountFunc);
      }

      public void Write(byte[] stuff) => Write(stuff, 0, stuff.Length);

      public void Write(byte[] stuff, int offset, int length) {
         Target.Write(stuff, offset, length);
         bananas.Add(new FredTheBanana { Length = length, Skip = 0 });
         byteCount += length;
      }

      public void CopyTo(Stream stream) {
         var buffer = Target.GetBuffer();
         int offset = 0;
         foreach (var banana in bananas) {
            stream.Write(buffer, offset, banana.Length);

            offset += banana.Length;
            offset += banana.Skip;
         }
      }

      public abstract class VarIntReservation : IDisposable {
         private readonly long varIntBlockOffset;
         protected readonly MemoryStream target;
         private readonly List<FredTheBanana> banans;
         private readonly int banansIndex;
         private readonly Action<int> addToByteCount;

         protected VarIntReservation(long varIntBlockOffset, MemoryStream target, List<FredTheBanana> banans, int banansIndex, Action<int> addToByteCount) {
            this.varIntBlockOffset = varIntBlockOffset;
            this.target = target;
            this.banans = banans;
            this.banansIndex = banansIndex;
            this.addToByteCount = addToByteCount;
         }

         public void SetValue(int value) {
            var restore = target.Position;

            target.Position = varIntBlockOffset;
            VarIntSerializer.WriteVariableInt(target.WriteByte, value);

            var varIntByteCount = (int)(target.Position - varIntBlockOffset);
            var fredTheBanana = banans[banansIndex];
            fredTheBanana.Length = varIntByteCount;
            fredTheBanana.Skip = kMaxLengthByteCount - varIntByteCount;
            banans[banansIndex] = fredTheBanana;
            addToByteCount(varIntByteCount);

            target.Position = restore;
         }

         public virtual void Dispose() { }
      }

      public class LengthReservation : VarIntReservation {
         private readonly Func<int> getBytesWritten;
         private readonly long initialBytesWritten;

         public LengthReservation(long varIntBlockOffset, MemoryStream target, List<FredTheBanana> banans, int banansIndex, Action<int> addToByteCount, Func<int> getBytesWritten) : base(varIntBlockOffset, target, banans, banansIndex, addToByteCount) {
            this.getBytesWritten = getBytesWritten;
            this.initialBytesWritten = getBytesWritten();
         }

         public override void Dispose() {
            SetValue((int)(getBytesWritten() - initialBytesWritten));
         }
      }

      public class CountReservation : VarIntReservation {
         public CountReservation(long varIntBlockOffset, MemoryStream target, List<FredTheBanana> banans, int banansIndex, Action<int> addToByteCount) : base(varIntBlockOffset, target, banans, banansIndex, addToByteCount) { }
      }
   }
}