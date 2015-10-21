# An Introduction to NMockito

NMockito is an open-source Mockito-inspired unit testing framework for the .net ecosystem released under the BSD License and maintained by [The Dargon Project](https://www.github.com/the-dargon-project) developer [ItzWarty](https://www.twitter.com/ItzWarty).

NMockito has two dependencies: Castle.Core (Proxying) and xUnit (For Test Running, Assertions). 

Supported features include out/ref-parameter mocking, spies, and untracked mocks. Upcoming features previously supported in NMockito include invocation order verification, argument captors.

# Some Highlights
Mocked fields:
```csharp
class TestClass : NMockitoInstance {
   [Mock] private readonly IDictionary<int, string> namesById = null;
}
```
Placeholders for test code:
```csharp
var user1Id = CreatePlaceholder<Guid>();     // {00000001-0000-0000-0000-000000000000}
var user1Name = CreatePlaceholder<string>(); // "placeholder_2"
```
Elegant poco training:
```csharp
var message = CreateMock<Message>(m =>
   m.Size == MessageDispatcher.kMessageSizeLimit &&
   m.Type == MessageType.Unknown);
```
With support for out/ref parameters:
```csharp
Expect<int, bool>(value => dictionary.TryGetValue(key, out value))
   .SetOut(1337).ThenReturn(true)
```
And automated proxy class testing:
```csharp
var tester = new StaticProxyBehaviorTester(this);
tester.TestStaticProxy(typeof(FileUtil));
```

# Documentation
Note: Newer C# language specifications permit static usings. Consider statically including NMockitoStatics and invoking ReinitializeMocks at your constructor rather than extending from NMockitoInstance.
## Creating a Mock
### Via Invocation:
```csharp
public class TestClass : NMockitoInstance {
   [Fact] public void Run() {
      AssertNotNull(CreateMock<TestInterface>()); // True!
   }
   public interface TestInterface { }
}
```
### Via Reflection:
```csharp
public class TestClass : NMockitoInstance {
   [Mock] private readonly TestInterface mock = null;
   [Fact] public void Run() {
      AssertNotNull(mock); // True!
   }
   public interface TestInterface { }
}
```
## Simple Mock Training
Given the following interface:
```csharp
public interface Message {
   int Size { get; }
   MessageType Type { get; } 
}
```

### Training with When()
We can teach and validate our mock's behavior as follows:
```csharp
public class TestClass : NMockitoInstance {
   [Mock] private readonly Message message = null;
   [Fact] public void Run() {
      // Train the Mock
      When(message.Size).ThenReturn(MessageDispatcher.kMessageSizeLimit);
      // Interact with it like a normal object.
      AssertEquals(MessageDispatcher.kMessageSizeLimit, message.Size);
      // Verify we read the Size property (NoOp necessary as member accesses aren't statements)
      Verify(message).Size.NoOp();
      // And that nothing else happened to our mock:
      VerifyNoMoreInteractions();
   }
}
```
### Training with Expectations
Of course, the When and Verify seem redundant. We have better means to express this!
```csharp
public class TestClass : NMockitoInstance {
   [Mock] private readonly Message message = null;
   [Fact] public void Run() {
      // Train the Mock
      Expect(message.Size).ThenReturn(MessageDispatcher.kMessageSizeLimit);
      // Interact with it like a normal object.
      AssertEquals(MessageDispatcher.kMessageSizeLimit, message.Size);
      // Verify that our trained calls were invoked.
      VerifyExpectationsAndNoMoreInteractions();
   }
}
```
#### We can even train the mock's expectations fluently:
```csharp
message.Size.IsEqualTo(MessageDispatcher.kMessageSizeLimit);
message.Type.IsEqualTo(MessageType.Ping);
```
#### And sometimes, with magic!
```csharp
var message = CreateMock<Message>(m =>
   m.Size == MessageDispatcher.kMessageSizeLimit &&
   m.Type == MessageType.Ping);
```

## Mocking Ref/Out Parameters:
NMockito supports mocking C#'s `ref` and `out` parameters! Observe the following code:
```csharp
var key = CreatePlaceholder<int>();
var value = CreatePlaceholder<string>();
var mock = CreateMock<IReadOnlyDictionary<int, string>>();

Expect<string, bool>(x => mock.TryGetValue(key, out x))
   .SetOut(null).ThenReturn(false)
   .SetOut(value).ThenReturn(true);

string result;
AssertFalse(mock.TryGetValue(key, out result));
AssertNull(result);

AssertTrue(mock.TryGetValue(key, out result));
AssertEquals(value, result);
```
Mocking `ref` parameters is visually identical.

## Internal and Private Interfaces
We can test internal interfaces just like public interfaces. However, to generate mocks the assemblies containing internal interfaces must have the following code in AssemblyInfo.cs:

```csharp
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
```

NMockito cannot mock private interfaces, as a workaround, make your interfaces internal.
