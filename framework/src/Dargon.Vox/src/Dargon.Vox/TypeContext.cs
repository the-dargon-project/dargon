using System;

namespace Dargon.Vox {
   public class TypeContext {
      public TypeContext(Type type, Func<object> activator) {
         Type = type;
         Activator = activator;
      }

      public Type Type { get; }
      public Func<object> Activator { get; }

      public static TypeContext Create<T>() => Create(typeof(T));
      public static TypeContext Create(Type type) => Create(type, GetActivator(type));
      public static TypeContext Create(Type type, Func<object> activator) => new TypeContext(type, activator);

      private static Func<object> GetActivator(Type type) {
         return () => System.Activator.CreateInstance(type);
      }
   }
}