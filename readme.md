# An Introduction to Courier

Courier is a work-in-progress open-source networking library for the .net ecosystem released under Version 3 of the GNU Public License and maintained by [The Dargon Project](https://www.github.com/the-dargon-project) developer [ItzWarty](https://www.twitter.com/ItzWarty).

Courier has multiple dependencies listed [here](https://github.com/the-dargon-project/Dargon.Courier/blob/master/Dargon.Courier.Impl/packages.config).

# Installing Courier
Courier is released as a NuGet package via the Dargon Package Source.

* Add `https://nuget.dargon.io/` as a NuGet package source.
* Run `Install-Package Dargon.Courier.Api` from the package management console on non-startup projects.
* Run `Install-Package Dargon.Courier.Impl` from the package management console on your startup project.

# Documentation
## Creating a Courier Client
After adding Dargon.Courier.Impl as a dependency to your startup application, you can construct a courier client as follows:
``` csharp
// Initialize IoC Container
var ryu = new RyuFactory().Create();
ryu.Setup();

// Construct Courier Client
var courierClientFactory = ryu.Get<CourierClientFactory>();
var courierClient = courierClientFactory.CreateUdpCourierClient(kPort);
```
Your courier client is now listening for incoming messages on UDP port `kPort`!

## Peer Discovery:
The courier client periodically announces itself on the local network to enable peer discovery. To facilitate peer discovery and diagnostics, courier clients may publish information describing theirselves as follows:
```
courierClient.SetProperty(kOperatingSystem, Environment.OSVersion.ToString());
courierClient.SetProperty(kConnectedUserCount, connectedUserCount);
```
foreach (var peer in courierClient.EnumeratePeers()) {
   
}

You can view this information as follows:
```csharp
foreach (var peer in courierClient.EnumeratePeers()) {
   var os = peer.GetProperty<string>(kOperatingSystem);
   var userCount = peer.GetProperty<string>(kConnectedUserCount);
   Console.WriteLine($"{peer.Name} {peer.Identifier} {os} {userCount}");
}
```

## Receiving Messages
Assuming the given data-transfer object (hooked-up via Dargon.PortableObjects):
``` csharp
[Autoserializable]
public class AuditStateDto : IPortableObject {
   public bool IsOkay { get; set; }
   public string Status { get; set; }
}

var auditState = new AuditStateDto { IsOkay = true, Status = "Derp!" };
```

You can asynchronously receive and process messages as follows:
```csharp
courierClient.RegisterPayloadHandler<AuditStateDto>(HandleAuditState);

void HandleAuditState(IReceivedMessage<AuditStateDto> x) {
   var payload = x.Payload;
   Console.WriteLine($"{x.SenderId} {payload.IsOkay} {payload.Status}");
}
```

## Sending Messages
Courier supports three modes of data transfer: Unreliable Unicast, Reliable Unicast, and Unreliable Broadcast. All messages are potentially delivered out-of-order.

Reliable messages may be transmitted at a low, medium, or high priority.

**Reliable Unicast:** `courierClient.SendReliableUnicast(recipientId, auditState, messagePriority);`  
**Unreliable Unicast:** `courierClient.SendUnreliableUnicast(recipientId, auditState);`  
**Unreliable Broadcast:** `courierClient.SendBroadcast(auditState);`

## Additional Stuff
### Routing a Message to Payload Handlers
You may route a message to your client's payload handlers as follows:

``` csharp
var receivedMessageFactory = ryu.Get<ReceivedMessageFactory>();
var courierMessageFactory = ryu.Get<CourierMessageFactory>();
courierClient.RouteMessage(receivedMessageFactory.CreateReceivedMessage(
   senderId,
   courierMessageFactory.CreateMessage(
      recipient,
      messageFlags,
      payload
   ),
   IPAddress.Loopback));
```

This can be useful for testing your message routing, if necessary.

### Reading information of Remote Courier Node by Id
We probably could have published the audit info through Courier's node state. If we'd done so, reading the info would look as follows:
``` csharp
var remoteEndpoint = courierClient.GetRemoteCourierEndpointOrNull(Guid.Empty);
var remoteAddress = remoteEndpoint.LastAddress;
var remoteName = remoteEndpoint.Name;
var remoteAuditInfo = remoteEndpoint.GetProperty<AuditInfoDto>(kAuditInfoKey);
```