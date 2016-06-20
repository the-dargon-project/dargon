﻿using Dargon.Vox.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Dargon.Vox {
   public interface IBodyWriter {
      void Write<T>(T val);
   }

   public interface IBodyReader {
      T Read<T>();
   }

   public interface ITypeSerializer { }

   public interface ITypeSerializer<T> : ITypeSerializer {
      void Serialize(IBodyWriter writer, T source);
      void Deserialize(IBodyReader reader, T target);
   }

   public interface ISerializableType {
      void Serialize(IBodyWriter writer);
      void Deserialize(IBodyReader reader);
   }

   public class InlineTypeSerializer<T> : ITypeSerializer<T> {
      private readonly Action<IBodyWriter, T> write;
      private readonly Action<IBodyReader, T> read;

      public InlineTypeSerializer(Action<IBodyWriter, T> write, Action<IBodyReader, T> read) {
         this.write = write;
         this.read = read;
      }

      public Type Type => typeof(T);

      public void Serialize(IBodyWriter writer, T source) {
         write(writer, source);
      }

      public void Deserialize(IBodyReader reader, T target) {
         read(reader, target);
      }
   }

   public static class AutoTypeSerializerFactory {
      private delegate void EmitterFunc(ILGenerator ilGenerator);

      private static readonly IGenericFlyweightFactory<EmitterFunc> emitWriteCallFuncs
         = GenericFlyweightFactory.ForMethod<EmitterFunc>(
            typeof(ReaderWriterCallHelper<>), nameof(ReaderWriterCallHelper<object>.EmitWrite));


      private static readonly IGenericFlyweightFactory<EmitterFunc> emitReadCallFuncs
         = GenericFlyweightFactory.ForMethod<EmitterFunc>(
            typeof(ReaderWriterCallHelper<>), nameof(ReaderWriterCallHelper<object>.EmitRead));

      public static IReadOnlyDictionary<Type, ITypeSerializer> CreateMany(IReadOnlyList<Type> types) {
         return types.ToDictionary(type => type, Create);
      }

      public static ITypeSerializer<T> Create<T>() => (ITypeSerializer<T>)Create(typeof(T));

      public static ITypeSerializer Create(Type type) {
         var serialize = new DynamicMethod($"__serialize_{type.FullName}", typeof(void), new[] { typeof(IBodyWriter), type }, type);
         var deserialize = new DynamicMethod($"__deserialize_{type.FullName}", typeof(void), new[] { typeof(IBodyReader), type }, type);

         var serializeEmitter = serialize.GetILGenerator();
         var deserializeEmitter = deserialize.GetILGenerator();

         var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
         var fieldsToSerialize = type.GetFields(bindingFlags).Where(x => !x.IsInitOnly);
         foreach (var fieldToSerialize in fieldsToSerialize) {
            EmitFieldSerialization(serializeEmitter, fieldToSerialize);
            EmitFieldDeserialization(deserializeEmitter, fieldToSerialize);
         }

         serializeEmitter.Emit(OpCodes.Ret);
         deserializeEmitter.Emit(OpCodes.Ret);

         var serializerType = typeof(AutoTypeSerializer<>).MakeGenericType(type);
         var serializerConstructor = serializerType.GetConstructors()[0];
         var ctorParams = serializerConstructor.GetParameters();
         var serializeMethodType = ctorParams[0].ParameterType;
         var deserializeMethodType = ctorParams[1].ParameterType;
         return (ITypeSerializer)Activator.CreateInstance(
            serializerType,
            serialize.CreateDelegate(serializeMethodType),
            deserialize.CreateDelegate(deserializeMethodType));
      }

      private static void EmitFieldSerialization(ILGenerator serializeEmitter, FieldInfo field) {
         serializeEmitter.Emit(OpCodes.Ldarg_0);

         // Push subject-field value onto evaluation stack.
         serializeEmitter.Emit(OpCodes.Ldarg_1);
         serializeEmitter.Emit(OpCodes.Ldfld, field);

         // Push call of Write-To-Slot method.
         emitWriteCallFuncs.Get(field.FieldType)(serializeEmitter);
      }

      private static void EmitFieldDeserialization(ILGenerator deserializeEmitter, FieldInfo field) {
         // Push subject onto stack
         deserializeEmitter.Emit(OpCodes.Ldarg_1);

         // Push invocation target (slot reader) and slot number onto evaluation stack
         deserializeEmitter.Emit(OpCodes.Ldarg_0);

         // Invoke reader method, which places read result onto evaluation stack
         emitReadCallFuncs.Get(field.FieldType)(deserializeEmitter);

         // Store the read value to subject's field
         deserializeEmitter.Emit(OpCodes.Stfld, field);
      }

      private class AutoTypeSerializer<T> : InlineTypeSerializer<T> {
         public AutoTypeSerializer(Action<IBodyWriter, T> write, Action<IBodyReader, T> read) : base(write, read) { }

         public override bool Equals(object obj) {
            return GetType() == obj?.GetType();
         }
      }

      private static class ReaderWriterCallHelper<T> {
         private static readonly MethodInfo writerMethod;
         private static readonly MethodInfo readerMethod;

         static ReaderWriterCallHelper() {
            var slotWriterType = typeof(IBodyWriter);
            var slotReaderType = typeof(IBodyReader);
            var writeMethodDefinition = slotWriterType.GetMethods().First(m => m.Name == nameof(IBodyWriter.Write));
            var readMethodDefinition = slotReaderType.GetMethods().First(m => m.Name == nameof(IBodyReader.Read));
            writerMethod = writeMethodDefinition.MakeGenericMethod(typeof(T));
            readerMethod = readMethodDefinition.MakeGenericMethod(typeof(T));
         }

         public static void EmitWrite(ILGenerator ilGenerator) {
            ilGenerator.Emit(OpCodes.Call, writerMethod);
         }

         public static void EmitRead(ILGenerator ilGenerator) {
            ilGenerator.Emit(OpCodes.Call, readerMethod);
         }
      }
   }
}
