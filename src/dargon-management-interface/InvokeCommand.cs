using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dargon.Commons;
using Dargon.Repl;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dargon.Courier.Management.UI {
   public class InvokeCommand : ICommand {
      public string Name => "invoke";

      public int Eval(string args) {
         string methodName;
         args = Tokenizer.Next(args, out methodName);

         SomeNode methodNode;
         if (!ReplGlobals.Current.TryGetChild(methodName, out methodNode)) {
            throw new Exception($"Couldn't find method of name {methodNode}.");
         }

         var methodDto = methodNode.MethodDto;
         object[] parameters = methodDto.Parameters.Map(
            p => {
               string arg;
               args = Tokenizer.Next(args, out arg);
               return Convert.ChangeType(arg, p.Type);
            });

         Console.WriteLine($"Invokeing {methodName} with params ({parameters.Join(", ")}.");

         var mobDto = methodNode.Parent.MobDto;
         var result = ReplGlobals.ManagementObjectService.InvokeManagedOperation(mobDto.FullName, methodName, parameters);

         Console.WriteLine("Result: ");
         PrintResult(result);
         return 0;
      }

      private void PrintResult(object result) {
         if (result == null) {
            Console.WriteLine("[null]");
         } else if (result is string || result.GetType().IsValueType) {
            Console.WriteLine(result);
         } else {
            var toString = result.GetType().GetMethod("ToString");
            if (toString.DeclaringType != typeof(object)) {
               Console.WriteLine(result);
            } else {
               string json = JsonConvert.SerializeObject(
                  result, Formatting.Indented,
                  new JsonConverter[] { new StringEnumConverter() });
               Console.WriteLine(json);
            }
         }
      }
   }
}
