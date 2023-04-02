﻿
                                                                                                                        // <auto-generated/>

                                                                                                                        using Dargon.Commons;

                                                                                                                        using Dargon.Vox2;

                                                                                                                        using System;

                                                                                                                        using System.Numerics;

                                                                                                                        namespace Dargon.Courier.ManagementTier.Vox {
                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxType(kTypeId, VanityRedirectFromType = typeof(ManagementDataSetDto<>))]
                                                                                                                           public sealed partial class ManagementDataSetDtoSerializer<T> : IVoxSerializer<ManagementDataSetDto<T>> {
                                                                                                                              public const int kTypeId = 46;

                                                                                                                              private static ManagementDataSetDtoSerializer<T> s_instance;
                                                                                                                              public static ManagementDataSetDtoSerializer<T> GetInstance(VoxSerializerContainer vsc) => s_instance ??= new(vsc);
                                                                                                                              public static ManagementDataSetDtoSerializer<T> GetInstance(VoxContext vox) => GetInstance(vox.SerializerContainer);

                                                                                                                              private IVoxSerializer<T> _dep_T;
private IVoxSerializer<Dargon.Courier.ManagementTier.Vox.DataPoint<T>> _dep_Dargon_Courier_ManagementTier_Vox_DataPoint1337T1338;


                                                                                                                              public ManagementDataSetDtoSerializer(VoxSerializerContainer vsc) {
                                                                                                                                 _dep_T = vsc.GetSerializerForType<T>();
_dep_Dargon_Courier_ManagementTier_Vox_DataPoint1337T1338 = vsc.GetSerializerForType<Dargon.Courier.ManagementTier.Vox.DataPoint<T>>();


                                                                                                                                 SimpleTypeId = kTypeId;
                                                                                                                                 FullTypeId = Arrays.Concat(new[] { kTypeId }, _dep_T.FullTypeId);
                                                                                                                                 FullTypeIdBytes = FullTypeId.ToVariableIntBytes();
                                                                                                                              }

                                                                                                                              public int SimpleTypeId { get; }
                                                                                                                              public int[] FullTypeId { get; }
                                                                                                                              public byte[] FullTypeIdBytes { get; }

                                                                                                                              public bool IsUpdatable => true;

                                                                                                                              public void WriteTypeIdBytes(VoxWriter writer) => writer.WriteTypeIdBytes(FullTypeIdBytes);
                                                                                                                              public void AssertReadTypeId(VoxReader reader) => reader.AssertReadTypeIdBytes(FullTypeIdBytes);
                                                                                                                              public void WriteFull(VoxWriter writer, ref ManagementDataSetDto<T> self) { ManagementDataSetDtoSerializer<T>.GetInstance(writer.Context).WriteTypeIdBytes(writer); WriteRaw(writer, ref self);
 }
                                                                                                                              public void WriteRaw(VoxWriter writer, ref ManagementDataSetDto<T> self) { 
                                                                                                                                  {
var _voxvarname_arr = self.DataPoints;
if (_voxvarname_arr == null) {
   writer.WriteRawInt32((int)-1);
} else {
   writer.WriteRawInt32(_voxvarname_arr.Length);
   for (var _voxvarname_arr_i = 0; _voxvarname_arr_i < _voxvarname_arr.Length; _voxvarname_arr_i++) {
      var _voxvarname_arr_el_copy = _voxvarname_arr[_voxvarname_arr_i];
      _dep_Dargon_Courier_ManagementTier_Vox_DataPoint1337T1338.WriteRaw(writer, ref _voxvarname_arr_el_copy);
   }
}

                                                                                                                                  }
 }
               

                                                                                                                              public ManagementDataSetDto<T> ReadFull(VoxReader reader) { ManagementDataSetDto<T> res = new(); ReadFullIntoRef(reader, ref res); return res; }
                                                                                                                              public ManagementDataSetDto<T> ReadRaw(VoxReader reader) { ManagementDataSetDto<T> res = new(); ReadRawIntoRef(reader, ref res); return res; }
                                                                                                                              public void ReadFullIntoRef(VoxReader reader, ref ManagementDataSetDto<T> self) { ManagementDataSetDtoSerializer<T>.GetInstance(reader.Context).AssertReadTypeId(reader); ReadRawIntoRef(reader, ref self);
 }
                                                                                                                              public void ReadRawIntoRef(VoxReader reader, ref ManagementDataSetDto<T> self) { if (self == null) throw new ArgumentNullException(nameof(ManagementDataSetDto<T>));

                                                                                                                                  {
var _voxvarname_arr_count = reader.ReadRawInt32();
var _voxvarname_arr = _voxvarname_arr_count == -1 ? null : new Dargon.Courier.ManagementTier.Vox.DataPoint<T>[_voxvarname_arr_count];
for (var _voxvarname_arr_i = 0; _voxvarname_arr_i < _voxvarname_arr_count; _voxvarname_arr_i++) {
   _voxvarname_arr[_voxvarname_arr_i] = _dep_Dargon_Courier_ManagementTier_Vox_DataPoint1337T1338.ReadRaw(reader);;
}

                                                                                                                                     self.DataPoints = _voxvarname_arr;
                                                                                                                                  }
 }
                  

                                                                                                                              void IVoxSerializer.WriteRawObject(VoxWriter writer, object val) { ManagementDataSetDto<T> v = (ManagementDataSetDto<T>)val; WriteRaw(writer, ref v); }
                                                                                                                              object IVoxSerializer.ReadRawObject(VoxReader reader) => ReadRaw(reader);
                                                                                                                           }
               

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxInternalsAutoSerializedTypeInfoAttribute(GenericSerializerTypeDefinition = typeof(ManagementDataSetDtoSerializer<>))]
                                                                                                                           public partial class ManagementDataSetDto<T> /* : IVoxCustomType<Dargon.Courier.ManagementTier.Vox.ManagementDataSetDto<T>> */ {
                                                                                                                              /* can't put more here because of enums */
                                                                                                                           }


                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public static class VoxGenerated_ManagementDataSetDtoStatics {
                                                                                                                              internal static ManagementDataSetDtoSerializer<T> GetSerializerInstance<T>(VoxContext vox) => ManagementDataSetDtoSerializer<T>.GetInstance(vox.SerializerContainer);

                                                                                                                              public static void WriteFullManagementDataSetDto<T>(this VoxWriter writer, Dargon.Courier.ManagementTier.Vox.ManagementDataSetDto<T> value) { var copy = value; GetSerializerInstance<T>(writer.Context).WriteFull(writer, ref value); }
                                                                                                                              public static void WriteRawManagementDataSetDto<T>(this VoxWriter writer, Dargon.Courier.ManagementTier.Vox.ManagementDataSetDto<T> value) { var copy = value; GetSerializerInstance<T>(writer.Context).WriteRaw(writer, ref value); }
                                                                                                                              public static ManagementDataSetDto<T> ReadFullManagementDataSetDto<T>(this VoxReader reader) => GetSerializerInstance<T>(reader.Context).ReadFull(reader);
                                                                                                                              public static ManagementDataSetDto<T> ReadRawManagementDataSetDto<T>(this VoxReader reader) => GetSerializerInstance<T>(reader.Context).ReadRaw(reader);
            

                                                                                                                              public static void WriteFullInto<T>(this ManagementDataSetDto<T> self, VoxWriter writer) { var copy = self; GetSerializerInstance<T>(writer.Context).WriteFull(writer, ref copy); }
                                                                                                                              public static void WriteRawInto<T>(this ManagementDataSetDto<T> self, VoxWriter writer) { var copy = self; GetSerializerInstance<T>(writer.Context).WriteRaw(writer, ref copy); }
            

                                                                                                                              public static void ReadFullFrom<T>( this ManagementDataSetDto<T> self, VoxReader reader) { var copy = self; GetSerializerInstance<T>(reader.Context).ReadFullIntoRef(reader, ref copy); }
                                                                                                                              public static void ReadRawFrom<T>( this ManagementDataSetDto<T> self, VoxReader reader) { var copy = self; GetSerializerInstance<T>(reader.Context).ReadRawIntoRef(reader, ref copy); }
               


                                                                                                                           }
                                                                                                                        }

                                                                                                                        namespace Dargon.Courier.ManagementTier.Vox {
               

                                                                                                                        }