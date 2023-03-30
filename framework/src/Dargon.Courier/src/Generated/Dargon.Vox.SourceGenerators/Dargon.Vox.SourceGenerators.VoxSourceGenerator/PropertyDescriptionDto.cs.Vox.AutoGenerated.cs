﻿
                                                                                                                        // <auto-generated/>

                                                                                                                        using Dargon.Commons;

                                                                                                                        using Dargon.Vox2;

                                                                                                                        using System;

                                                                                                                        using System.Numerics;

                                                                                                                        namespace Dargon.Courier.ManagementTier.Vox {
                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxType(kTypeId, VanityRedirectFromType = typeof(PropertyDescriptionDto))]
                                                                                                                           public sealed partial class PropertyDescriptionDtoSerializer : IVoxSerializer<PropertyDescriptionDto> {
                                                                                                                              public const int kTypeId = 43;

                                                                                                                              private static PropertyDescriptionDtoSerializer s_instance;
                                                                                                                              public static PropertyDescriptionDtoSerializer GetInstance(VoxSerializerContainer vsc) => s_instance ??= new(vsc);
                                                                                                                              public static PropertyDescriptionDtoSerializer GetInstance(VoxContext vox) => GetInstance(vox.SerializerContainer);

                                                                                                                              

                                                                                                                              public PropertyDescriptionDtoSerializer(VoxSerializerContainer vsc) {
                                                                                                                                 

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
                                                                                                                              public void WriteFull(VoxWriter writer, ref PropertyDescriptionDto self) { PropertyDescriptionDtoSerializer.GetInstance(writer.Context).WriteTypeIdBytes(writer); WriteRaw(writer, ref self);
 }
                                                                                                                              public void WriteRaw(VoxWriter writer, ref PropertyDescriptionDto self) { 
                                                                                                                                  {
writer.WriteRawString(self.Name);

                                                                                                                                  }

                                                                                                                                  {
writer.WriteRawType(self.Type);

                                                                                                                                  }

                                                                                                                                  {
writer.WriteRawBoolean(self.HasGetter);

                                                                                                                                  }

                                                                                                                                  {
writer.WriteRawBoolean(self.HasSetter);

                                                                                                                                  }
 }
               

                                                                                                                              public PropertyDescriptionDto ReadFull(VoxReader reader) { PropertyDescriptionDto res = new(); ReadFullIntoRef(reader, ref res); return res; }
                                                                                                                              public PropertyDescriptionDto ReadRaw(VoxReader reader) { PropertyDescriptionDto res = new(); ReadRawIntoRef(reader, ref res); return res; }
                                                                                                                              public void ReadFullIntoRef(VoxReader reader, ref PropertyDescriptionDto self) { PropertyDescriptionDtoSerializer.GetInstance(reader.Context).AssertReadTypeId(reader); ReadRawIntoRef(reader, ref self);
 }
                                                                                                                              public void ReadRawIntoRef(VoxReader reader, ref PropertyDescriptionDto self) { if (self == null) throw new ArgumentNullException(nameof(PropertyDescriptionDto));

                                                                                                                                  {

                                                                                                                                     self.Name = reader.ReadRawString();
                                                                                                                                  }

                                                                                                                                  {

                                                                                                                                     self.Type = reader.ReadRawType();
                                                                                                                                  }

                                                                                                                                  {

                                                                                                                                     self.HasGetter = reader.ReadRawBoolean();
                                                                                                                                  }

                                                                                                                                  {

                                                                                                                                     self.HasSetter = reader.ReadRawBoolean();
                                                                                                                                  }
 }
                  

                                                                                                                              void IVoxSerializer.WriteRawObject(VoxWriter writer, object val) { PropertyDescriptionDto v = (PropertyDescriptionDto)val; WriteRaw(writer, ref v); }
                                                                                                                              object IVoxSerializer.ReadRawObject(VoxReader reader) => ReadRaw(reader);
                                                                                                                           }
               

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxInternalsAutoSerializedTypeInfoAttribute(GenericSerializerTypeDefinition = typeof(PropertyDescriptionDtoSerializer))]
                                                                                                                           public partial class PropertyDescriptionDto /* : IVoxCustomType<Dargon.Courier.ManagementTier.Vox.PropertyDescriptionDto> */ {
                                                                                                                              private static PropertyDescriptionDtoSerializer GetSerializerInstance(VoxContext vox) => PropertyDescriptionDtoSerializer.GetInstance(vox.SerializerContainer);
                                                                                                                              // public PropertyDescriptionDtoSerializer Serializer => PropertyDescriptionDtoSerializer.Instance;
                                                                                                                              // IVoxSerializer IVoxCustomType.Serializer => Serializer;
                                                                                                                              // IVoxSerializer<Dargon.Courier.ManagementTier.Vox.PropertyDescriptionDto> IVoxCustomType<Dargon.Courier.ManagementTier.Vox.PropertyDescriptionDto>.Serializer => Serializer;

                                                                                                                              public void WriteFullInto(VoxWriter writer) { var copy = this; GetSerializerInstance(writer.Context).WriteFull(writer, ref copy); }
                                                                                                                              public void WriteRawInto(VoxWriter writer) { var copy = this; GetSerializerInstance(writer.Context).WriteRaw(writer, ref copy); }
                  

                                                                                                                              public void ReadFullFrom(VoxReader reader) { var copy = this; GetSerializerInstance(reader.Context).ReadFullIntoRef(reader, ref copy); }
                                                                                                                              public void ReadRawFrom(VoxReader reader) { var copy = this; GetSerializerInstance(reader.Context).ReadRawIntoRef(reader, ref copy); }
                     

                                                                                                                           }


                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public static class VoxGenerated_PropertyDescriptionDtoStatics {
                                                                                                                              public static void WriteFullPropertyDescriptionDto(this VoxWriter writer, Dargon.Courier.ManagementTier.Vox.PropertyDescriptionDto value) { var copy = value; PropertyDescriptionDtoSerializer.GetInstance(writer.Context).WriteFull(writer, ref value); }
                                                                                                                              public static void WriteRawPropertyDescriptionDto(this VoxWriter writer, Dargon.Courier.ManagementTier.Vox.PropertyDescriptionDto value) { var copy = value; PropertyDescriptionDtoSerializer.GetInstance(writer.Context).WriteRaw(writer, ref value); }
                                                                                                                              public static PropertyDescriptionDto ReadFullPropertyDescriptionDto(this VoxReader reader) => PropertyDescriptionDtoSerializer.GetInstance(reader.Context).ReadFull(reader);
                                                                                                                              public static PropertyDescriptionDto ReadRawPropertyDescriptionDto(this VoxReader reader) => PropertyDescriptionDtoSerializer.GetInstance(reader.Context).ReadRaw(reader);
                                                                                                                           }
                                                                                                                        }

                                                                                                                        namespace Dargon.Courier.ManagementTier.Vox {
               

                                                                                                                        }
