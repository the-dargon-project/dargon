namespace Dargon.Vox2 {
   public enum BuiltInVoxTypeIds {
      Null = -0x00,
      Void = -0x01,
      Type = -0x02,
      Object = -0x03,
      String = -0x04,

      Array1 = -0x0A,
      List = -0x0B,
      ExposedArrayList = -0x0C,
      HashSet = -0x0D,
      Dictionary = -0x0E,
      ExposedListDictionary = -0x0F,

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

      Vector2 = -0x20,
      Vector3 = -0x21,
      Vector4 = -0x22,

      DateTime = -0x2A,
      DateTimeOffset = -0x2B,
      TimeSpan = -0x2C,

      ValueTuple0 = -0x30,
      ValueTuple1 = -0x31,
      ValueTuple2 = -0x32,
      ValueTuple3 = -0x33,
      ValueTuple4 = -0x34,

      KeyValuePair = -0x3A,

      // Below multi-byte when serialized (mag > 0x40)
      Array2 = -0xA0,
      Array3 = -0xA1,
      ValueTuple5 = -0xA2,
      ValueTuple6 = -0xA3,
      ValueTuple7 = -0xA4,
      ValueTuple8 = -0xA5,

      /// <summary>
      /// An invalid TypeID. Used to represent a bad typeId;
      /// do not use! Used internally.
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