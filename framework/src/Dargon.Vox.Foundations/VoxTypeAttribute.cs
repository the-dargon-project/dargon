using System;

namespace Dargon.Vox2 {
   public class VoxInternalBaseDummyType {}

   public class VoxInternalBaseAttribute : Attribute { }

   public interface IVoxCustomType {
      IVoxSerializer Serializer { get; }

      void WriteFullInto(VoxWriter writer);
      void WriteRawInto(VoxWriter writer);

      void ReadFullFrom(VoxReader reader);
      void ReadRawFrom(VoxReader reader);
   }

   public interface IVoxCustomType<TSelf> : IVoxCustomType {
      new IVoxSerializer<TSelf> Serializer { get; }
   }

   [Flags]
   public enum VoxTypeFlags : int {
      None = 0,
      StubFull = 1,
      StubRaw = 2,
      NonUpdatable = 4,
      Specialization = 8,
      NoCodeGen = 16,
   }

   [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
   public sealed class VoxTypeAttribute : VoxInternalBaseAttribute {
      public VoxTypeAttribute(int Id) {
         this.Id = Id;
      }

      public int Id { get; }
      public Type VanityRedirectFromType { get; set; }
      public Type RedirectToType { get; set; }
      public VoxTypeFlags Flags { get; set; }
   }

   [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
   public class DoNotSerializeAttribute : VoxInternalBaseAttribute {

   }
}