using Dargon.Commons;
using Dargon.Nest.Repl;
using Dargon.Repl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dargon.Nest {
   public class DispatcherCommand : ICommand, IDispatcher {
      private readonly Dictionary<string, ICommand> commandsByName = new Dictionary<string, ICommand>();
      private readonly string name;
      private IDispatcher dispatcher;

      public DispatcherCommand(string name) {
         this.name = name;
      }

      public void RegisterCommand(ICommand command) {
         commandsByName.Add(command.Name, command);
         if (command is IDispatcher) {
            ((IDispatcher)command).Parent = this;
         }
      }

      public IDispatcher Parent { get { return dispatcher; } set { dispatcher = value; } }
      public string FullName { get { return (dispatcher == null ? "" : (dispatcher.FullName + " ")) + name; } }
      public string Name { get { return name; } }

      public int Eval(string args) {
         string commandName;
         args = Tokenizer.Next(args, out commandName);

         if (string.IsNullOrWhiteSpace(commandName) || commandName.Equals("help", StringComparison.OrdinalIgnoreCase)) {
            PrettyPrint.List(commandsByName.Select(kvp => kvp.Key));
            return 0;
         }

         ICommand command;
         if (commandsByName.TryGetValue(commandName, out command)) {
            return command.Eval(args);
         } else {
            throw new CommandNotFoundException("Could not find command " + commandName + " in " + FullName);
         }
      }
   }
}