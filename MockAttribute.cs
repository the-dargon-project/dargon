using System;

namespace NMockito {
   public class MockAttribute : Attribute {
      private bool tracked;
      private Type staticType;

      public MockAttribute(Tracking tracking = Tracking.Tracked, Type staticType = null) {
         this.tracked = tracking == Tracking.Tracked;
         this.staticType = staticType;
      }

      public bool Tracked { get { return tracked; } set { tracked = value; } }
      public Type StaticType { get { return staticType; } set { staticType = value; } }
   }
}
