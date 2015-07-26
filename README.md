# An Introduction to NMockito
NMockito is an open-source Mockito-inspired unit testing framework for the .net ecosystem released under the BSD License and maintained by [The Dargon Project](https://www.github.com/the-dargon-project) developer [ItzWarty](https://www.twitter.com/ItzWarty).

NMockito has two dependencies: Castle.Core (Proxying) and xUnit (For Test Running). 

Supported features include out/ref-parameter mocking, argument captors, order/times verification, and untracked mocks. Only creates interface mocks.

# Example Code
Note: Newer C# language specifications permit static usings. Consider statically including NMockitoStatic and invoking ReinitializeMocks at your constructor rather than extending from NMockitoInstance.

## Event Subscription

Given classes:

```csharp
public class UserDisplay {
   private readonly IUserViewModel model;

   public UserDisplay(IUserViewModel model) {
      this.model = model;
   }

   public void Initialize() {
      model.PropertyChanged += HandleModelPropertyChanged;
   }

   internal void HandleModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
   }
}

public interface IUserViewModel : INotifyPropertyChanged {
   string Name { get; }
   int Age { get; }
}
```

Test code might look as follows:

```csharp
public class UserDisplayTests : NMockitoInstance {     
   [Mock] private readonly IUserViewModel model = null;
 
   private readonly UserDisplay testObj = null;

   public EventTests() {
      testObj = new UserDisplay(model);
   }
 
   [Fact]
   public void InitializeSubscribesToModelTest() {
      testObj.Initialize();
      Verify(model).PropertyChanged += testObj.HandleModelPropertyChanged;
      VerifyNoMoreInteractions();
   }
}
```

## Invocation Ordering
This method tests a class that's a bit too big, but it gets the point across. Verifying invocation timing/order is useful when, for example, working with asynchronous services or lazily evaluated IEnumerables.

```csharp
public class LeagueLifecycleServiceImplTests : NMockitoInstance
{
   private LeagueLifecycleServiceImpl testObj;

   [Mock] private readonly LeagueModificationRepositoryService leagueModificationRepositoryService = null;
   [Mock] private readonly InjectedModuleService injectedModuleService = null;
   [Mock] private readonly LeagueModificationResolutionService leagueModificationResolutionService = null;
   [Mock] private readonly LeagueModificationObjectCompilerService leagueModificationObjectCompilerService = null;
   [Mock] private readonly LeagueModificationCommandListCompilerService leagueModificationCommandListCompilerService = null;
   [Mock] private readonly LeagueGameModificationLinkerService leagueGameModificationLinkerService = null;
   [Mock] private readonly LeagueSessionService leagueSessionService = null;
   [Mock] private readonly RadsService radsService = null;
   [Mock] private readonly ILeagueInjectedModuleConfigurationFactory leagueInjectedModuleConfigurationFactory = null;
   
   [Mock] private readonly IModification firstModification = null;
   [Mock] private readonly IModification secondModification = null;
   private IEnumerable<IModification> modifications;

   public LeagueLifecycleServiceImplTests()
   {
      testObj = new LeagueLifecycleServiceImpl(injectedModuleService, leagueModificationRepositoryService, leagueModificationResolutionService, leagueModificationObjectCompilerService, leagueModificationCommandListCompilerService, leagueGameModificationLinkerService, leagueSessionService, radsService, leagueInjectedModuleConfigurationFactory);

      modifications = new[] { firstModification, secondModification };
      When(leagueModificationRepositoryService.EnumerateModifications()).ThenReturn(modifications);
   }

   [Fact]
   public void HandleUninitializedToPreclientPhaseTransitionTest()
   {
      var firstResolutionTask = CreateMock<IResolutionTask>();
      var secondResolutionTask = CreateMock<IResolutionTask>();
      When(leagueModificationResolutionService.StartModificationResolution(firstModification, ModificationTargetType.Client)).ThenReturn(firstResolutionTask);
      When(leagueModificationResolutionService.StartModificationResolution(secondModification, ModificationTargetType.Client)).ThenReturn(secondResolutionTask);
   
      var firstCompilationTask = CreateMock<ICompilationTask>();
      var secondCompilationTask = CreateMock<ICompilationTask>();
      When(leagueModificationObjectCompilerService.CompileObjects(firstModification, ModificationTargetType.Client)).ThenReturn(firstCompilationTask);
      When(leagueModificationObjectCompilerService.CompileObjects(secondModification, ModificationTargetType.Client)).ThenReturn(secondCompilationTask);
      
      testObj.HandleUninitializedToPreclientPhaseTransition(CreateMock<ILeagueSession>(), new LeagueSessionPhaseChangedArgs(0, 0));
   
      Verify(leagueModificationRepositoryService, Times(1)).EnumerateModifications();
      Verify(leagueModificationResolutionService, Times(1), AfterPrevious()).StartModificationResolution(firstModification, ModificationTargetType.Client);
      Verify(leagueModificationResolutionService, Times(1), WithPrevious()).StartModificationResolution(secondModification, ModificationTargetType.Client);
      Verify(radsService, Times(1), AfterPrevious()).Suspend();
      Verify(firstResolutionTask, Times(1), AfterPrevious()).WaitForChainCompletion();
      Verify(secondResolutionTask, Times(1), WithPrevious()).WaitForChainCompletion();
      Verify(leagueModificationObjectCompilerService, Times(1), AfterPrevious()).CompileObjects(firstModification, ModificationTargetType.Client);
      Verify(leagueModificationObjectCompilerService, Times(1), WithPrevious()).CompileObjects(secondModification, ModificationTargetType.Client);
      Verify(firstCompilationTask, Times(1), AfterPrevious()).WaitForChainCompletion();
      Verify(secondCompilationTask, Times(1), WithPrevious()).WaitForChainCompletion();
      VerifyNoMoreInteractions();
   }
}
```
## Smart Parameters (Argument Captors)

This unit test covers a constructor and method that do work. The constructor instantiates an object through `new` and passes it to another class. With an argument captor, we can capture that class, then validate its properties at a later time.

```csharp
public class ConnectorWorkerTests : NMockitoInstance {
   private readonly ConnectorWorker testObj;

   [Mock] private readonly IThreadingProxy threadingProxy = null;
   [Mock] private readonly IConnectorContext connectorContext = null;
   [Mock] private readonly ICancellationTokenSource cancellationTokenSource = null;
   [Mock] private readonly ISemaphore updateSemaphore = null;
   [Mock] private readonly IThread workerThread = null;
   [Mock] private readonly IConcurrentDictionary<string, IServiceContext> serviceContextsByName = null;
   [Mock] private readonly ICancellationToken cancellationToken = null;

   public ConnectorWorkerTests() {
      When(threadingProxy.CreateCancellationTokenSource()).ThenReturn(cancellationTokenSource);
      When(threadingProxy.CreateSemaphore(0, int.MaxValue)).ThenReturn(updateSemaphore);
      When(threadingProxy.CreateThread(Any<ThreadEntryPoint>(), Any<ThreadCreationOptions>())).ThenReturn(workerThread);

      this.testObj = new ConnectorWorker(threadingProxy, connectorContext);
      
      var threadCreationOptionsCaptor = new ArgumentCaptor<ThreadCreationOptions>();
      Verify(threadingProxy, Once()).CreateCancellationTokenSource();
      Verify(threadingProxy, Once()).CreateSemaphore(0, int.MaxValue);
      Verify(threadingProxy, Once()).CreateThread(Eq<ThreadEntryPoint>(testObj.ThreadEntryPoint), threadCreationOptionsCaptor.GetParameter());
      VerifyNoMoreInteractions();

      AssertTrue(threadCreationOptionsCaptor.Value.IsBackground);

      When(cancellationTokenSource.Token).ThenReturn(cancellationToken);
   }

   [Fact]
   public void InitializeHappyPathTest() {
      testObj.Initalize(serviceContextsByName);

      Verify(connectorContext).Initialize(serviceContextsByName);
      Verify(workerThread).Start();
      VerifyNoMoreInteractions();
   }
}
```   
## Smart Parameters (Ref/Out)

Ref and Out methods are tested in similar fashions. 

We can test this class and interface:

```csharp
public class ConfigurationManager {
   private readonly IReadOnlyDictionary<string, Configuration> configurationsByName;

   public ConfigurationManager(IReadOnlyDictionary<string, Configuration> configurationsByName) {
      this.configurationsByName = configurationsByName;
   }

   public bool TryValidateConfiguration(string key) {
      Configuration configuration;
      if (configurationsByName.TryGetValue(key, out configuration)) {
         return configuration.Validate();
      } else {
         return false;
      }
   }
}

public interface Configuration {
   bool Validate();
}
```

As follows:

```csharp
public class ConfigurationManagerTests : NMockitoInstance {
   [Mock] private readonly IReadOnlyDictionary<string, Configuration> configurationsByName = null;

   private readonly ConfigurationManager testObj;

   public OutRefTests() {
      testObj = new ConfigurationManager(configurationsByName);
   }

   [Fact]
   public void OutParameterTest() {
      const string kKeyName = "key_name";
      Configuration configuration = CreateMock<Configuration>();
      Configuration configurationPlaceholder = CreateMock<Configuration>();
      When(configuration.Validate()).ThenReturn(true);
      When(configurationsByName.TryGetValue(kKeyName, out configurationPlaceholder)).Set(configurationPlaceholder, configuration).ThenReturn(true);

      AssertTrue(testObj.TryValidateConfiguration(kKeyName));

      Verify(configuration).Validate();
      Verify(configurationsByName).TryGetValue(kKeyName, out configurationPlaceholder);
      VerifyNoMoreInteractions();
   }
}
```
## Internal and Private Interfaces
We can test internal interfaces just like public interfaces. However, to generate mocks the assemblies containing internal interfaces must have the following code in AssemblyInfo.cs:

```csharp
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
```

NMockito cannot mock private interfaces, as a workaround, make your interfaces internal.
