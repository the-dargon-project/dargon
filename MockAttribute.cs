using System;

namespace NMockito
{
   public class MockAttribute : Attribute
   {
      private bool tracked;

      public MockAttribute(Tracking tracking = Tracking.Tracked) {
         this.Tracked = tracking == Tracking.Tracked;
      }

      public bool Tracked { get { return tracked; } set { tracked = value; } }
   }
}
