using Dargon.Repl;

namespace Dargon.Courier.Management.Repl {
   public static class DargonManagementInterfaceReplStatics {
      public static DispatcherCommand RegisterDargonManagementInterfaceCommands(this DispatcherCommand dispatcher) {
         dispatcher.RegisterCommand(new UseCommand());
         dispatcher.RegisterCommand(new FetchMobsCommand());
         dispatcher.RegisterCommand(new FetchOperationsCommand());
         dispatcher.RegisterCommand(new ChangeDirectoryCommand());
         dispatcher.RegisterCommand(new ListDirectoryCommand());
         dispatcher.RegisterCommand(new InvokeCommand());
         dispatcher.RegisterCommand(new SetCommand());
         dispatcher.RegisterCommand(new GetCommand());
         dispatcher.RegisterCommand(new GraphCommand());
         dispatcher.RegisterCommand(new TreeCommand());
         dispatcher.RegisterCommand(new ExitCommand());
         dispatcher.RegisterCommand(new DispatcherMultiCommand(
            "a",
            new[] { "use tcp 127.0.0.1:21337", "fetch-mobs", "cd !!UdpDebugMob", "fetch-ops", "ls" },
            dispatcher));
         dispatcher.RegisterCommand(new DispatcherMultiCommand(
            "aa",
            new[] { "use tcp 127.0.0.1:21338", "fetch-mobs", "cd !!UdpDebugMob", "fetch-ops", "ls" },
            dispatcher));
         dispatcher.RegisterCommand(new DispatcherMultiCommand(
            "aaa",
            new[] { "use tcp 127.0.0.1:21339", "fetch-mobs", "cd !!UdpDebugMob", "fetch-ops", "ls" },
            dispatcher));
         return dispatcher;
      }
   }
}