using System;

namespace Dargon.Vox2 {
   public class VoxInternalBaseDummyType {}

   public class VoxInternalBaseAttribute : Attribute { }

   public class VoxInternalsAutoSerializedTypeInfoAttribute : Attribute {
      public Type GenericSerializerTypeDefinition { get; set; }
   }

   // public interface IVoxCustomType {
   //    IVoxSerializer Serializer { get; }
   //
   //    void WriteFullInto(VoxWriter writer);
   //    void WriteRawInto(VoxWriter writer);
   //
   //    void ReadFullFrom(VoxReader reader);
   //    void ReadRawFrom(VoxReader reader);
   // }

   // public interface IVoxCustomType<TSelf> : IVoxCustomType {
   //    new IVoxSerializer<TSelf> Serializer { get; }
   // }

   [Flags]
   public enum VoxTypeFlags : int {
      None = 0,

      /// <summary>
      /// The vox type's raw (no type header) and full (with-header)
      /// serialize/deserialize are stubbed by the code generator and
      /// must be implemented by user code.
      /// </summary>
      StubFull = 1,
      
      /// <summary>
      /// The vox type's raw (no type header) serialize/deserialize are stubbed
      /// by the code generator and must be implemented by user code.
      /// </summary>
      StubRaw = 2,

      /// <summary>
      /// The vox type cannot be updated in-place by-ref (e.g. it is a primitive,
      /// or it's a framework type whose internals we don't control).
      ///
      /// Generated stubs will return by-value, rather than potentially modifying
      /// an existing type instance in-place.
      /// </summary>
      NonUpdatable = 4,

      /// <summary>
      /// This is a specialized serializer for a particular combination of generic
      /// arguments for a given generic type.
      /// </summary>
      Specialization = 8,

      /// <summary>
      /// While the VoxType attribute is provided for runtime metadata, no codegen
      /// should execute; all expected generated members of the attribute will
      /// be manually specified.
      ///
      /// This is generally used internally to the library.
      /// </summary>
      NoCodeGen = 16,
   }

   /// <summary>
   /// Use this attribute to declare a partial type
   /// that benefits from Vox auto-member generation.
   ///
   /// By default, auto-serialization is opted-in.
   /// </summary>
   [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
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