using System.ComponentModel;
using Xunit;

namespace NMockito {
   public class EventTests : NMockitoInstance {
      private readonly UserDisplay testObj;

      [Mock] private readonly IUserViewModel model = null;

      public EventTests() {
         testObj = new UserDisplay(model);
      }

      [Fact]
      public void InitializeSubscribesToModelTest() {
         testObj.Initialize();
         Verify(model).PropertyChanged += testObj.HandleModelPropertyChanged;
         VerifyNoMoreInteractions();
      }

      public class UserDisplay {
         private readonly IUserViewModel model;

         public UserDisplay(IUserViewModel model) {
            this.model = model;
         }

         public void Initialize() {
            model.PropertyChanged += HandleModelPropertyChanged;
         }

         internal void HandleModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
         }
      }

      public interface IUserViewModel : INotifyPropertyChanged {
         string Name { get; }
         int Age { get; }
      }
   }
}
