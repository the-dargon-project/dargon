using System.Reflection;

namespace NMockito2.Mocks {
   public class MockAndMethod {
      public MockAndMethod(InvocationDescriptor invocationDescriptor) {
         Mock = invocationDescriptor.Mock;
         Method = invocationDescriptor.Method;
      }

      public object Mock { get; }
      public MethodInfo Method { get; }

      public override bool Equals(object obj) {
         var asMockAndMethod = obj as MockAndMethod;
         return asMockAndMethod != null &&
                Mock == asMockAndMethod.Mock &&
                Method == asMockAndMethod.Method;
      }

      public override int GetHashCode() {
         return Mock.GetHashCode() ^ Method.GetHashCode();
      }
   }
}