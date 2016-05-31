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

         Console.WriteLine($"Invoking {methodName} with params ({parameters.Join(", ")}).");

         var mobDto = methodNode.Parent.MobDto;
         var result = ReplGlobals.ManagementObjectService.InvokeManagedOperation(mobDto.FullName, methodName, parameters);

         Console.WriteLine("Result: ");
         ReplUtils.PrettyPrint(result);
         return 0;
      }
   }

   public class SetCommand : ICommand {
      public string Name => "set";

      public int Eval(string args) {
         string propertyName;
         args = Tokenizer.Next(args, out propertyName);

         SomeNode propertyNode;
         if (!ReplGlobals.Current.TryGetChild(propertyName, out propertyNode)) {
            throw new Exception($"Couldn't find property of name {propertyNode}.");
         }

         var propertyDto = propertyNode.PropertyDto;

         if (!propertyDto.HasSetter) {
            throw new Exception($"Property {propertyName} does not have a setter.");
         }

         string valueString;
         Tokenizer.Next(args, out valueString);
         object value = Convert.ChangeType(valueString, propertyDto.Type);

         var parameters = new[] { value };

         Console.WriteLine($"Invoking {propertyName} setter with params ({parameters.Join(", ")}.");

         var mobDto = propertyNode.Parent.MobDto;
         var result = ReplGlobals.ManagementObjectService.InvokeManagedOperation(mobDto.FullName, propertyName, parameters);

         Console.WriteLine("Result: ");
         ReplUtils.PrettyPrint(result);
         return 0;
      }
   }

   public class GetCommand : ICommand {
      public string Name => "get";

      public int Eval(string args) {
         string propertyName;
         args = Tokenizer.Next(args, out propertyName);

         SomeNode propertyNode;
         if (!ReplGlobals.Current.TryGetChild(propertyName, out propertyNode)) {
            throw new Exception($"Couldn't find property of name {propertyNode}.");
         }

         var propertyDto = propertyNode.PropertyDto;

         if (!propertyDto.HasGetter) {
            throw new Exception($"Property {propertyName} does not have a getter.");
         }

         var parameters = new object[0];

         Console.WriteLine($"Invoking {propertyName} getter with params ({parameters.Join(", ")}.");

         var mobDto = propertyNode.Parent.MobDto;
         var result = ReplGlobals.ManagementObjectService.InvokeManagedOperation(mobDto.FullName, propertyName, parameters);

         Console.WriteLine("Result: ");
         ReplUtils.PrettyPrint(result);
         return 0;
      }
   }
}
