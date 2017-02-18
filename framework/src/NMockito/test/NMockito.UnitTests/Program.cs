using System;
using System.Reflection;
using Xunit.Sdk;

namespace ConsoleApplication {
   public class Program {
      public static void Main(string[] args) {
         Console.WriteLine("Hi");
         var executor = new Executor(Assembly.GetExecutingAssembly().Location);
         var runAssembly = new Executor.RunAssembly(executor, null);
      }
   }
}
