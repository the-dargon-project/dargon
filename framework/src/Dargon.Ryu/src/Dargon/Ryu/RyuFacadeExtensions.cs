namespace Dargon.Ryu {
   public static class RyuFacadeExtensions {
      public static T Activate<T>(this IRyuFacade facade) {
         return (T)facade.Activate(typeof(T));
      }
   }
}