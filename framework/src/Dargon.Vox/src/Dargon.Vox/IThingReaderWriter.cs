using System.IO;
using Dargon.Commons.Exceptions;
using Dargon.Vox.Internals.TypePlaceholders;

namespace Dargon.Vox {
   public interface IThingReaderWriter {
      void WriteThing(SomeMemoryStreamWrapperThing dest, object subject);
      object ReadBody(VoxBinaryReader reader);
   }

   public class NullThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;

      public NullThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(TNull)));
      }

      public object ReadBody(VoxBinaryReader reader) {
         return null;
      }
   }

   public class SystemObjectThingReaderWriter : IThingReaderWriter {
      private readonly FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache;

      public SystemObjectThingReaderWriter(FullTypeBinaryRepresentationCache fullTypeBinaryRepresentationCache) {
         this.fullTypeBinaryRepresentationCache = fullTypeBinaryRepresentationCache;
      }

      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         dest.Write(fullTypeBinaryRepresentationCache.GetOrCompute(typeof(object)));
      }

      public object ReadBody(VoxBinaryReader reader) {
         return new object();
      }
   }

   public class VoidThingReaderWriter : IThingReaderWriter {
      public void WriteThing(SomeMemoryStreamWrapperThing dest, object subject) {
         throw new InvalidStateException("Attempted to write void thing");
      }

      public object ReadBody(VoxBinaryReader reader) {
         throw new InvalidStateException("Attempted to read void thing");
      }
   }
}