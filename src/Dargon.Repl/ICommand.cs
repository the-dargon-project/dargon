namespace Dargon.Repl {
   public interface ICommand {
      string Name { get; }
      int Eval(string args);
   }

   public interface IDispatcher {
      string FullName { get; }
      string Name { get; }
      int Eval(string args);
      IDispatcher Parent { get; set; }
   }
}
