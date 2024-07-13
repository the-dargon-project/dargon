﻿
                                                                                                                        // <auto-generated/>

                                                                                                                        using Dargon.Commons;

                                                                                                                        using Dargon.Vox2;

                                                                                                                        using System;

                                                                                                                        using System.Numerics;

                                                                                                                        namespace Dargon.Courier.AuditingTier {
                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxType(kTypeId, VanityRedirectFromType = typeof(AggregateStatistics<>))]
                                                                                                                           public sealed partial class AggregateStatisticsSerializer<T> : IVoxSerializer<AggregateStatistics<T>> {
                                                                                                                              public const int kTypeId = 48;

                                                                                                                              private static AggregateStatisticsSerializer<T> s_instance;
                                                                                                                              public static AggregateStatisticsSerializer<T> GetInstance(VoxSerializerContainer vsc) => s_instance ??= new(vsc);
                                                                                                                              public static AggregateStatisticsSerializer<T> GetInstance(VoxContext vox) => GetInstance(vox.SerializerContainer);

                                                                                                                              private IVoxSerializer<T> _dep_T;


                                                                                                                              public AggregateStatisticsSerializer(VoxSerializerContainer vsc) {
                                                                                                                                 _dep_T = vsc.GetSerializerForType<T>();


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
                                                                                                                              public void WriteFull(VoxWriter writer, ref AggregateStatistics<T> self) { AggregateStatisticsSerializer<T>.GetInstance(writer.Context).WriteTypeIdBytes(writer); WriteRaw(writer, ref self);
 }
                                                                                                                              public void WriteRaw(VoxWriter writer, ref AggregateStatistics<T> self) { 
                                                                                                                                  {
var _voxvarname_copy = self.Sum;
_dep_T.WriteRaw(writer, ref _voxvarname_copy);

                                                                                                                                  }

                                                                                                                                  {
var _voxvarname_copy = self.Min;
_dep_T.WriteRaw(writer, ref _voxvarname_copy);

                                                                                                                                  }

                                                                                                                                  {
var _voxvarname_copy = self.Max;
_dep_T.WriteRaw(writer, ref _voxvarname_copy);

                                                                                                                                  }

                                                                                                                                  {
writer.WriteRawInt32(self.Count);

                                                                                                                                  }

                                                                                                                                  {
var _voxvarname_copy = self.Average;
_dep_T.WriteRaw(writer, ref _voxvarname_copy);

                                                                                                                                  }
 }
               

                                                                                                                              public AggregateStatistics<T> ReadFull(VoxReader reader) { AggregateStatistics<T> res = new(); ReadFullIntoRef(reader, ref res); return res; }
                                                                                                                              public AggregateStatistics<T> ReadRaw(VoxReader reader) { AggregateStatistics<T> res = new(); ReadRawIntoRef(reader, ref res); return res; }
                                                                                                                              public void ReadFullIntoRef(VoxReader reader, ref AggregateStatistics<T> self) { AggregateStatisticsSerializer<T>.GetInstance(reader.Context).AssertReadTypeId(reader); ReadRawIntoRef(reader, ref self);
 }
                                                                                                                              public void ReadRawIntoRef(VoxReader reader, ref AggregateStatistics<T> self) { if (self == null) throw new ArgumentNullException(nameof(AggregateStatistics<T>));

                                                                                                                                  {

                                                                                                                                     self.Sum = _dep_T.ReadRaw(reader);;
                                                                                                                                  }

                                                                                                                                  {

                                                                                                                                     self.Min = _dep_T.ReadRaw(reader);;
                                                                                                                                  }

                                                                                                                                  {

                                                                                                                                     self.Max = _dep_T.ReadRaw(reader);;
                                                                                                                                  }

                                                                                                                                  {

                                                                                                                                     self.Count = reader.ReadRawInt32();
                                                                                                                                  }

                                                                                                                                  {

                                                                                                                                     self.Average = _dep_T.ReadRaw(reader);;
                                                                                                                                  }
 }
                  

                                                                                                                              void IVoxSerializer.WriteRawObject(VoxWriter writer, object val) { AggregateStatistics<T> v = (AggregateStatistics<T>)val; WriteRaw(writer, ref v); }
                                                                                                                              object IVoxSerializer.ReadRawObject(VoxReader reader) => ReadRaw(reader);
                                                                                                                           }
               

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           [VoxInternalsAutoSerializedTypeInfoAttribute(GenericSerializerTypeDefinition = typeof(AggregateStatisticsSerializer<>))]
                                                                                                                           public partial class AggregateStatistics<T> /* : IVoxCustomType<Dargon.Courier.AuditingTier.AggregateStatistics<T>> */ {
                                                                                                                              /* can't put more here because of enums */
                                                                                                                           }

                                                                                                                        }

                                                                                                                        namespace Dargon.Courier.AuditingTier {

                                                                                                                           /// <summary>Autogenerated</summary>
                                                                                                                           public static class VoxGenerated_AggregateStatisticsStatics {
                                                                                                                              internal static AggregateStatisticsSerializer<T> GetSerializerInstance<T>(VoxContext vox) => AggregateStatisticsSerializer<T>.GetInstance(vox.SerializerContainer);

                                                                                                                              public static void WriteFullAggregateStatistics<T>(this VoxWriter writer, Dargon.Courier.AuditingTier.AggregateStatistics<T> value) { var copy = value; GetSerializerInstance<T>(writer.Context).WriteFull(writer, ref value); }
                                                                                                                              public static void WriteRawAggregateStatistics<T>(this VoxWriter writer, Dargon.Courier.AuditingTier.AggregateStatistics<T> value) { var copy = value; GetSerializerInstance<T>(writer.Context).WriteRaw(writer, ref value); }
                                                                                                                              public static AggregateStatistics<T> ReadFullAggregateStatistics<T>(this VoxReader reader) => GetSerializerInstance<T>(reader.Context).ReadFull(reader);
                                                                                                                              public static AggregateStatistics<T> ReadRawAggregateStatistics<T>(this VoxReader reader) => GetSerializerInstance<T>(reader.Context).ReadRaw(reader);
            

                                                                                                                              public static void WriteFullInto<T>(this AggregateStatistics<T> self, VoxWriter writer) { var copy = self; GetSerializerInstance<T>(writer.Context).WriteFull(writer, ref copy); }
                                                                                                                              public static void WriteRawInto<T>(this AggregateStatistics<T> self, VoxWriter writer) { var copy = self; GetSerializerInstance<T>(writer.Context).WriteRaw(writer, ref copy); }
            

                                                                                                                              public static void ReadFullFrom<T>( this AggregateStatistics<T> self, VoxReader reader) { var copy = self; GetSerializerInstance<T>(reader.Context).ReadFullIntoRef(reader, ref copy); }
                                                                                                                              public static void ReadRawFrom<T>( this AggregateStatistics<T> self, VoxReader reader) { var copy = self; GetSerializerInstance<T>(reader.Context).ReadRawIntoRef(reader, ref copy); }
               


                                                                                                                           }
                                                                                                                        }

                                                                                                                        // Stubs
                                                                                                                        namespace Dargon.Courier.AuditingTier {
               

                                                                                                                        }
