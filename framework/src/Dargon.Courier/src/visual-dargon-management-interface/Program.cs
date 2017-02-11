using System;
using System.Windows.Forms;
using Dargon.Courier.Management.GUI.Views;

namespace Dargon.Courier.Management.GUI {
   public class Program {
      public static void Main(string[] args) {
         Console.WriteLine("Hello World!");
         Application.EnableVisualStyles();
         Application.Run(new ConnectionWindow());
      }
   }
}
