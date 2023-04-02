﻿
                                                                                                                        // <auto-generated/>

                                                                                                                        using Dargon.Commons;

                                                                                                                        using Dargon.Courier.ManagementTier;

                                                                                                                        using Dargon.Vox2;

                                                                                                                        using System;

                                                                                                                        using System.Numerics;

                                                                                                                        namespace Dargon.Courier.ManagementTier {
                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxType(kTypeId, VanityRedirectFromType = typeof(MethodDescriptionDto))]
                                                                                                                           public sealed partial class MethodDescriptionDtoSerializer : IVoxSerializer<MethodDescriptionDto> {
                                                                                                                              public const int kTypeId = 42;

                                                                                                                              private static MethodDescriptionDtoSerializer s_instance;
                                                                                                                              public static MethodDescriptionDtoSerializer GetInstance(VoxSerializerContainer vsc) => s_instance ??= new(vsc);
                                                                                                                              public static MethodDescriptionDtoSerializer GetInstance(VoxContext vox) => GetInstance(vox.SerializerContainer);

                                                                                                                              

                                                                                                                              public MethodDescriptionDtoSerializer(VoxSerializerContainer vsc) {
                                                                                                                                 

                                                                                                                                 SimpleTypeId = kTypeId;
                                                                                                                                 FullTypeId = new[] { kTypeId };
                                                                                                                                 FullTypeIdBytes = FullTypeId.ToVariableIntBytes();
                                                                                                                              }

                                                                                                                              public int SimpleTypeId { get; }
                                                                                                                              public int[] FullTypeId { get; }
                                                                                                                              public byte[] FullTypeIdBytes { get; }

                                                                                                                              public bool IsUpdatable => true;

                                                                                                                              public void WriteTypeIdBytes(VoxWriter writer) => writer.WriteTypeIdBytes(FullTypeIdBytes);
                                                                                                                              public void AssertReadTypeId(VoxReader reader) => reader.AssertReadTypeIdBytes(FullTypeIdBytes);
                                                                                                                              public void WriteFull(VoxWriter writer, ref MethodDescriptionDto self) { MethodDescriptionDtoSerializer.GetInstance(writer.Context).WriteTypeIdBytes(writer); WriteRaw(writer, ref self);
 }
                                                                                                                              public void WriteRaw(VoxWriter writer, ref MethodDescriptionDto self) { 
                                                                                                                                  {
writer.WriteRawString(self.Name);

                                                                                                                                  }

                                                                                                                                  {
var _voxvarname_arr = self.Parameters;
if (_voxvarname_arr == null) {
   writer.WriteRawInt32((int)-1);
} else {
   writer.WriteRawInt32(_voxvarname_arr.Count);
   foreach (var _voxvarname_arr_el in _voxvarname_arr) {
      writer.WriteRawParameterDescriptionDto(_voxvarname_arr_el);
   }
}

                                                                                                                                  }

                                                                                                                                  {
writer.WriteRawType(self.ReturnType);

                                                                                                                                  }
 }
               

                                                                                                                              public MethodDescriptionDto ReadFull(VoxReader reader) { MethodDescriptionDto res = new(); ReadFullIntoRef(reader, ref res); return res; }
                                                                                                                              public MethodDescriptionDto ReadRaw(VoxReader reader) { MethodDescriptionDto res = new(); ReadRawIntoRef(reader, ref res); return res; }
                                                                                                                              public void ReadFullIntoRef(VoxReader reader, ref MethodDescriptionDto self) { MethodDescriptionDtoSerializer.GetInstance(reader.Context).AssertReadTypeId(reader); ReadRawIntoRef(reader, ref self);
 }
                                                                                                                              public void ReadRawIntoRef(VoxReader reader, ref MethodDescriptionDto self) { if (self == null) throw new ArgumentNullException(nameof(MethodDescriptionDto));

                                                                                                                                  {

                                                                                                                                     self.Name = reader.ReadRawString();
                                                                                                                                  }

                                                                                                                                  {
var _voxvarname_arr_count = reader.ReadRawInt32();
var _voxvarname_arr = _voxvarname_arr_count == -1 ? null : new System.Collections.Generic.List<Dargon.Courier.ManagementTier.ParameterDescriptionDto>(_voxvarname_arr_count);
for (var _voxvarname_arr_i = 0; _voxvarname_arr_i < _voxvarname_arr_count; _voxvarname_arr_i++) {
   _voxvarname_arr.Add(reader.ReadRawParameterDescriptionDto());
}

                                                                                                                                     self.Parameters = _voxvarname_arr;
                                                                                                                                  }

                                                                                                                                  {

                                                                                                                                     self.ReturnType = reader.ReadRawType();
                                                                                                                                  }
 }
                  

                                                                                                                              void IVoxSerializer.WriteRawObject(VoxWriter writer, object val) { MethodDescriptionDto v = (MethodDescriptionDto)val; WriteRaw(writer, ref v); }
                                                                                                                              object IVoxSerializer.ReadRawObject(VoxReader reader) => ReadRaw(reader);
                                                                                                                           }
               

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxInternalsAutoSerializedTypeInfoAttribute(GenericSerializerTypeDefinition = typeof(MethodDescriptionDtoSerializer))]
                                                                                                                           public partial class MethodDescriptionDto /* : IVoxCustomType<Dargon.Courier.ManagementTier.MethodDescriptionDto> */ {
                                                                                                                              /* can't put more here because of enums */
                                                                                                                           }


                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public static class VoxGenerated_MethodDescriptionDtoStatics {
                                                                                                                              internal static MethodDescriptionDtoSerializer GetSerializerInstance(VoxContext vox) => MethodDescriptionDtoSerializer.GetInstance(vox.SerializerContainer);

                                                                                                                              public static void WriteFullMethodDescriptionDto(this VoxWriter writer, Dargon.Courier.ManagementTier.MethodDescriptionDto value) { var copy = value; GetSerializerInstance(writer.Context).WriteFull(writer, ref value); }
                                                                                                                              public static void WriteRawMethodDescriptionDto(this VoxWriter writer, Dargon.Courier.ManagementTier.MethodDescriptionDto value) { var copy = value; GetSerializerInstance(writer.Context).WriteRaw(writer, ref value); }
                                                                                                                              public static MethodDescriptionDto ReadFullMethodDescriptionDto(this VoxReader reader) => GetSerializerInstance(reader.Context).ReadFull(reader);
                                                                                                                              public static MethodDescriptionDto ReadRawMethodDescriptionDto(this VoxReader reader) => GetSerializerInstance(reader.Context).ReadRaw(reader);
            

                                                                                                                              public static void WriteFullInto(this MethodDescriptionDto self, VoxWriter writer) { var copy = self; GetSerializerInstance(writer.Context).WriteFull(writer, ref copy); }
                                                                                                                              public static void WriteRawInto(this MethodDescriptionDto self, VoxWriter writer) { var copy = self; GetSerializerInstance(writer.Context).WriteRaw(writer, ref copy); }
            

                                                                                                                              public static void ReadFullFrom( this MethodDescriptionDto self, VoxReader reader) { var copy = self; GetSerializerInstance(reader.Context).ReadFullIntoRef(reader, ref copy); }
                                                                                                                              public static void ReadRawFrom( this MethodDescriptionDto self, VoxReader reader) { var copy = self; GetSerializerInstance(reader.Context).ReadRawIntoRef(reader, ref copy); }
               


                                                                                                                           }
                                                                                                                        }

                                                                                                                        namespace Dargon.Courier.ManagementTier {
               

                                                                                                                        }