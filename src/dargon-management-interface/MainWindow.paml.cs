using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace PerspexApplication1 {
   public class MainWindow : Window {
      public MainWindow() {
         this.InitializeComponent();
         App.AttachDevTools(this);
      }

      private void InitializeComponent() {
         PerspexXamlLoader.Load(this);
      }
   }
}
