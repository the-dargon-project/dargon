using Dargon.Vox.Utilities;

namespace Dargon.Vox {
   public class FrameWriter {
      private readonly ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing;

      public FrameWriter(ThisIsTotesTheRealLegitThingReaderWriterThing thisIsTotesTheRealLegitThingReaderWriterThing) {
         this.thisIsTotesTheRealLegitThingReaderWriterThing = thisIsTotesTheRealLegitThingReaderWriterThing;
      }

      public void WriteFrame(SomeMemoryStreamWrapperThing dest, object subject) {
         using (dest.ReserveLength()) {
            thisIsTotesTheRealLegitThingReaderWriterThing.WriteThing(dest, subject);
         }
      }
   }
}