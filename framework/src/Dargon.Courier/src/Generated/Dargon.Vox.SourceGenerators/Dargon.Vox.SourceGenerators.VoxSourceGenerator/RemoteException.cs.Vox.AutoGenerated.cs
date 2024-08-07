﻿
                                                                                                                        // <auto-generated/>

                                                                                                                        using Dargon.Commons;

                                                                                                                        using Dargon.Vox2;

                                                                                                                        using System;

                                                                                                                        using System.Numerics;

                                                                                                                        namespace Dargon.Courier.ServiceTier.Client {
                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxType(kTypeId, VanityRedirectFromType = typeof(RemoteException))]
                                                                                                                           public sealed partial class RemoteExceptionSerializer : IVoxSerializer<RemoteException> {
                                                                                                                              public const int kTypeId = 32;

                                                                                                                              private static RemoteExceptionSerializer s_instance;
                                                                                                                              public static RemoteExceptionSerializer GetInstance(VoxSerializerContainer vsc) => s_instance ??= new(vsc);
                                                                                                                              public static RemoteExceptionSerializer GetInstance(VoxContext vox) => GetInstance(vox.SerializerContainer);

                                                                                                                              

                                                                                                                              public RemoteExceptionSerializer(VoxSerializerContainer vsc) {
                                                                                                                                 

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
                                                                                                                              public void WriteFull(VoxWriter writer, ref RemoteException self) { RemoteExceptionSerializer.GetInstance(writer.Context).WriteTypeIdBytes(writer); WriteRaw(writer, ref self);
 }
                                                                                                                              public void WriteRaw(VoxWriter writer, ref RemoteException self) { global::Dargon.Courier.ServiceTier.Client.RemoteException.Stub_WriteRaw_RemoteException(writer, self);
 }
               

                                                                                                                              public RemoteException ReadFull(VoxReader reader) { RemoteException res = new(); ReadFullIntoRef(reader, ref res); return res; }
                                                                                                                              public RemoteException ReadRaw(VoxReader reader) { RemoteException res = new(); ReadRawIntoRef(reader, ref res); return res; }
                                                                                                                              public void ReadFullIntoRef(VoxReader reader, ref RemoteException self) { RemoteExceptionSerializer.GetInstance(reader.Context).AssertReadTypeId(reader); ReadRawIntoRef(reader, ref self);
 }
                                                                                                                              public void ReadRawIntoRef(VoxReader reader, ref RemoteException self) { global::Dargon.Courier.ServiceTier.Client.RemoteException.Stub_ReadRawIntoRef_RemoteException(reader, ref self);
 }
                  

                                                                                                                              void IVoxSerializer.WriteRawObject(VoxWriter writer, object val) { RemoteException v = (RemoteException)val; WriteRaw(writer, ref v); }
                                                                                                                              object IVoxSerializer.ReadRawObject(VoxReader reader) => ReadRaw(reader);
                                                                                                                           }
               

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxInternalsAutoSerializedTypeInfoAttribute(GenericSerializerTypeDefinition = typeof(RemoteExceptionSerializer))]
                                                                                                                           public partial class RemoteException /* : IVoxCustomType<Dargon.Courier.ServiceTier.Client.RemoteException> */ {
                                                                                                                              /* can't put more here because of enums */
                                                                                                                           }

                                                                                                                        }

                                                                                                                        namespace Dargon.Courier.ServiceTier.Client {

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public static class VoxGenerated_RemoteExceptionStatics {
                                                                                                                              internal static RemoteExceptionSerializer GetSerializerInstance(VoxContext vox) => RemoteExceptionSerializer.GetInstance(vox.SerializerContainer);

                                                                                                                              public static void WriteFullRemoteException(this VoxWriter writer, Dargon.Courier.ServiceTier.Client.RemoteException value) { var copy = value; GetSerializerInstance(writer.Context).WriteFull(writer, ref value); }
                                                                                                                              public static void WriteRawRemoteException(this VoxWriter writer, Dargon.Courier.ServiceTier.Client.RemoteException value) { var copy = value; GetSerializerInstance(writer.Context).WriteRaw(writer, ref value); }
                                                                                                                              public static RemoteException ReadFullRemoteException(this VoxReader reader) => GetSerializerInstance(reader.Context).ReadFull(reader);
                                                                                                                              public static RemoteException ReadRawRemoteException(this VoxReader reader) => GetSerializerInstance(reader.Context).ReadRaw(reader);
            

                                                                                                                              public static void WriteFullInto(this RemoteException self, VoxWriter writer) { var copy = self; GetSerializerInstance(writer.Context).WriteFull(writer, ref copy); }
                                                                                                                              public static void WriteRawInto(this RemoteException self, VoxWriter writer) { var copy = self; GetSerializerInstance(writer.Context).WriteRaw(writer, ref copy); }
            

                                                                                                                              public static void ReadFullFrom( this RemoteException self, VoxReader reader) { var copy = self; GetSerializerInstance(reader.Context).ReadFullIntoRef(reader, ref copy); }
                                                                                                                              public static void ReadRawFrom( this RemoteException self, VoxReader reader) { var copy = self; GetSerializerInstance(reader.Context).ReadRawIntoRef(reader, ref copy); }
               


                                                                                                                           }
                                                                                                                        }

                                                                                                                        // Stubs
                                                                                                                        namespace Dargon.Courier.ServiceTier.Client {
               

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public /* nonstatic */ partial class RemoteException {
                                                                                                                              public static partial void Stub_WriteRaw_RemoteException(VoxWriter writer, RemoteException value);
public static partial void Stub_ReadRawIntoRef_RemoteException(VoxReader reader, ref RemoteException value);
                                                               
                                                                                                                           }

                                                                                                                        }
