namespace Dargon.Vox2 {
   public enum BuiltInVoxTypeIds {
      Null = -0x00,
      Void = -0x01,
      ListLike = -0x02,
      DictLike = -0x03,
      KeyValuePair = -0x04,
      Object = -0x05,
      Type = -0x06,

      Bool = -0x10,
      BoolTrue = -0x11,
      BoolFalse = -0x12,

      Int8 = -0x13,
      Int16 = -0x14,
      Int32 = -0x15,
      Int64 = -0x16,
      UInt8 = -0x17,
      UInt16 = -0x18,
      UInt32 = -0x19,
      UInt64 = -0x1A,

      Float = -0x1B,
      Double = -0x1C,
      Guid = -0x1D,
      DateTime = -0x1E,
      TimeSpan = -0x1F,

      String = -0x20,

      ValueTuple0 = -0x30,
      ValueTuple1 = -0x31,
      ValueTuple2 = -0x32,
      ValueTuple3 = -0x33,
      ValueTuple4 = -0x34,
      ValueTuple5 = -0x35,
      ValueTuple6 = -0x36,
      ValueTuple7 = -0x37,
      ValueTuple8 = -0x38,

      /// <summary>
      /// An invalid TypeID. Used to represent a bad typeId;
      /// do not use!
      /// </summary>
      Invalid = int.MinValue,

      /// <summary>
      /// Reserved for test. Duplicate usage is not guarded.
      /// Do not use this in real code, even tests!
      /// </summary>
      ReservedForInternalVoxTest0 = int.MinValue + 1,
      ReservedForInternalVoxTest1 = int.MinValue + 2,
      ReservedForInternalVoxTest2 = int.MinValue + 3,
   }
}