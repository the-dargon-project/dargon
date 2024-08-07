﻿
                                                                                                                        // <auto-generated/>

                                                                                                                        using Dargon.Commons;

                                                                                                                        using Dargon.Vox2;

                                                                                                                        using System;

                                                                                                                        using System.Numerics;

                                                                                                                        namespace Dargon.Courier.Vox {
                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxType(kTypeId, VanityRedirectFromType = typeof(MessageDto))]
                                                                                                                           public sealed partial class MessageDtoSerializer : IVoxSerializer<MessageDto> {
                                                                                                                              public const int kTypeId = 1;

                                                                                                                              private static MessageDtoSerializer s_instance;
                                                                                                                              public static MessageDtoSerializer GetInstance(VoxSerializerContainer vsc) => s_instance ??= new(vsc);
                                                                                                                              public static MessageDtoSerializer GetInstance(VoxContext vox) => GetInstance(vox.SerializerContainer);

                                                                                                                              

                                                                                                                              public MessageDtoSerializer(VoxSerializerContainer vsc) {
                                                                                                                                 

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
                                                                                                                              public void WriteFull(VoxWriter writer, ref MessageDto self) { MessageDtoSerializer.GetInstance(writer.Context).WriteTypeIdBytes(writer); WriteRaw(writer, ref self);
 }
                                                                                                                              public void WriteRaw(VoxWriter writer, ref MessageDto self) { 
                                                                                                                                  {
writer.WriteRawGuid(self.SenderId);

                                                                                                                                  }

                                                                                                                                  {
writer.WriteRawGuid(self.ReceiverId);

                                                                                                                                  }

                                                                                                                                  {
writer.WritePolymorphic<object>(self.Body);

                                                                                                                                  }
 }
               

                                                                                                                              public MessageDto ReadFull(VoxReader reader) { MessageDto res = new(); ReadFullIntoRef(reader, ref res); return res; }
                                                                                                                              public MessageDto ReadRaw(VoxReader reader) { MessageDto res = new(); ReadRawIntoRef(reader, ref res); return res; }
                                                                                                                              public void ReadFullIntoRef(VoxReader reader, ref MessageDto self) { MessageDtoSerializer.GetInstance(reader.Context).AssertReadTypeId(reader); ReadRawIntoRef(reader, ref self);
 }
                                                                                                                              public void ReadRawIntoRef(VoxReader reader, ref MessageDto self) { if (self == null) throw new ArgumentNullException(nameof(MessageDto));

                                                                                                                                  {

                                                                                                                                     self.SenderId = reader.ReadRawGuid();
                                                                                                                                  }

                                                                                                                                  {

                                                                                                                                     self.ReceiverId = reader.ReadRawGuid();
                                                                                                                                  }

                                                                                                                                  {

                                                                                                                                     self.Body = reader.ReadPolymorphic<object>();
                                                                                                                                  }
 }
                  

                                                                                                                              void IVoxSerializer.WriteRawObject(VoxWriter writer, object val) { MessageDto v = (MessageDto)val; WriteRaw(writer, ref v); }
                                                                                                                              object IVoxSerializer.ReadRawObject(VoxReader reader) => ReadRaw(reader);
                                                                                                                           }
               

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxInternalsAutoSerializedTypeInfoAttribute(GenericSerializerTypeDefinition = typeof(MessageDtoSerializer))]
                                                                                                                           public partial class MessageDto /* : IVoxCustomType<Dargon.Courier.Vox.MessageDto> */ {
                                                                                                                              /* can't put more here because of enums */
                                                                                                                           }

                                                                                                                        }

                                                                                                                        namespace Dargon.Courier.Vox {

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public static class VoxGenerated_MessageDtoStatics {
                                                                                                                              internal static MessageDtoSerializer GetSerializerInstance(VoxContext vox) => MessageDtoSerializer.GetInstance(vox.SerializerContainer);

                                                                                                                              public static void WriteFullMessageDto(this VoxWriter writer, Dargon.Courier.Vox.MessageDto value) { var copy = value; GetSerializerInstance(writer.Context).WriteFull(writer, ref value); }
                                                                                                                              public static void WriteRawMessageDto(this VoxWriter writer, Dargon.Courier.Vox.MessageDto value) { var copy = value; GetSerializerInstance(writer.Context).WriteRaw(writer, ref value); }
                                                                                                                              public static MessageDto ReadFullMessageDto(this VoxReader reader) => GetSerializerInstance(reader.Context).ReadFull(reader);
                                                                                                                              public static MessageDto ReadRawMessageDto(this VoxReader reader) => GetSerializerInstance(reader.Context).ReadRaw(reader);
            

                                                                                                                              public static void WriteFullInto(this MessageDto self, VoxWriter writer) { var copy = self; GetSerializerInstance(writer.Context).WriteFull(writer, ref copy); }
                                                                                                                              public static void WriteRawInto(this MessageDto self, VoxWriter writer) { var copy = self; GetSerializerInstance(writer.Context).WriteRaw(writer, ref copy); }
            

                                                                                                                              public static void ReadFullFrom( this MessageDto self, VoxReader reader) { var copy = self; GetSerializerInstance(reader.Context).ReadFullIntoRef(reader, ref copy); }
                                                                                                                              public static void ReadRawFrom( this MessageDto self, VoxReader reader) { var copy = self; GetSerializerInstance(reader.Context).ReadRawIntoRef(reader, ref copy); }
               


                                                                                                                           }
                                                                                                                        }

                                                                                                                        // Stubs
                                                                                                                        namespace Dargon.Courier.Vox {
               

                                                                                                                        }
