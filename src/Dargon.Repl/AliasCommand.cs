namespace Dargon.Repl {
   public class AliasCommand : ICommand {
      public AliasCommand(string name, ICommand target) {
         Name = name;
         Target = target;
      }

      public string Name { get; }
      public ICommand Target { get; }

      public int Eval(string args) {
         return Target.Eval(args);
      }
   }
}
