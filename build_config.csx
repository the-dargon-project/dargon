var projects = Projects("courier");
var dependencies = Projects("commons", "ryu", "vox", "NMockito");

Export.Solution(
   Name: "Dargon Courier",
   Commands: new ICommand[] {
      Build.Projects(projects, dependencies),
      Test.Projects(projects)
   }
);
