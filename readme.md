# An Introduction to Courier

Courier is a work-in-progress open-source library for the .net ecosystem released under Version 3 of the GNU Public License. It supports both message-passing and remote method invocation on top of TCP and UDP. It also supports exposing management objects, counters, and aggregators, which are useful for debugging and tuning live applications.

# Some Highlights
You can use dargon-management-interface (dmi) to connect to your live application and debug its state.
```
> graph outbound_bytes
══════════════════════ Dargon.Courier.TransportTier.Udp.Management.UdpDebugMob.inbound_bytes ═══════════════════════
             Min                                    Max                                  Sum
     ▄▄                       │245.20               ▄           ▄   │8999.30            ▄               │2614475.70
                              │                  ▄▄▄  ▄▄▄▄▄▄▄▄ ▄  ▄ │                                   │
         ▄     ▄              │                                     │                                   │
                              │                                  ▄ ▄│                                   │
                              │                                     │                                   │
                        ▄     │                      ▄              │                                   │
▄                             │                                     │                     ▄             │
               ▄              │                ▄                    │                                   │
             ▄                │                               ▄     │                         ▄▄        │
                              │                                     │                 ▄▄ ▄  ▄▄  ▄▄  ▄  ▄│
    ▄     ▄▄▄ ▄ ▄▄▄▄▄▄▄▄ ▄▄▄▄▄│        ▄   ▄▄▄                      │         ▄  ▄▄▄   ▄  ▄▄      ▄▄ ▄▄ │
                              │2.80                                 │-688.30                            │-237188.70
──────────────────────────────┘        ─────────────────────────────┘         ──────────────────────────┘
34.7s ago           810.4ms ago        34.7s ago          817.4ms ago         34.7s ago       826.4ms ago
^: 225.00, v: 23.00, µ: 68.07          ^: 8192.00, v: 119.00, µ: 6064.68      ^: 2376837.00, v: 450.00, µ: 341414.5

                    Average                                                   Count
                   ▄                           │6459.69                                ▄                 │1096.50
                ▄                              │                                        ▄▄ ▄             │
                                               │                                              ▄          │
                         ▄                     │                        ▄                   ▄            │
                  ▄  ▄                         │                                    ▄                ▄  ▄│
                      ▄                        │                                     ▄             ▄  ▄  │
                                               │                             ▄                 ▄         │
                                               │                               ▄                         │
                                               │                                                         │
                       ▄    ▄           ▄▄     │                           ▄                      ▄      │
▄     ▄ ▄▄▄   ▄           ▄  ▄▄ ▄▄▄ ▄▄ ▄   ▄▄ ▄│         ▄     ▄ ▄▄ ▄     ▄   ▄  ▄▄             ▄        │
                                               │-457.43                                                  │-97.50
───────────────────────────────────────────────┘         ────────────────────────────────────────────────┘
34.7s ago                            833.4ms ago         34.7s ago                             842.9ms ago
^: 5883.26, v: 119.00, µ: 1333.61                        ^: 997.00, v: 2.00, µ: 407.04
```
You can even fetch objects! (Nowadays JSON would be dumped instead)
```
> invoke GetMessage
Result:
[Message SenderId=06c43af7-8e3b-490a-9e85-482d21cac6f4, ReceiverId=ffec6014-8fec-4a75-b90c-6a3c7a257f59, Body=System.Object[]]
```
Or pass parameters:
```
> invoke SayHello test
Result:
Hello, test!
```
# Documentation
## Creating a Courier Client
After adding courier as a dependency to your startup application, you can construct a courier client as follows.
``` csharp
var builder = CourierBuilder.Create();
var client = await builder.UseUdpTransport();
                          .UseTcpServerTransport(21337);
                          .BuildAsync();
```
Your application's now listening to tcp/udp port 21337! You can use UseUdpTransport's overload to receive over unicast, too!
```
var config = UdpTransportConfigurationBuilder.Create().WithUnicastReceivePort(21339).Build()
builder.UseUdpTransport(config);
...
```
## UDP Peer Discovery:
Courier's transports handle peer discovery on their own. Clients may additionally publish information describing theirselves as follows:
```csharp
client.SetProperty(kOperatingSystem, Environment.OSVersion.ToString());
client.SetProperty(kConnectedUserCount, connectedUserCount);
```
You can view this information as follows:
```csharp
foreach (var peer in client.EnumeratePeers()) {
   var os = peer.GetProperty<string>(kOperatingSystem);
   var userCount = peer.GetProperty<string>(kConnectedUserCount);
   Console.WriteLine($"{peer.Name} {peer.Identifier} {os} {userCount}");
}
```

## Receiving Messages
Assuming the given data-transfer object (hooked-up via vox):
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

void HandleAuditState(IInboundMessageEvent<AuditStateDto> x) {
   var body = x.Body;
   Console.WriteLine($"{x.SenderId} {body.IsOkay} {body.Status}");
}
```

## Sending Messages
Over UDP, Courier supports three modes of data transfer: Unreliable Unicast, Reliable Unicast, and Unreliable Broadcast. All messages are potentially delivered out-of-order.

No longer in rewrite: Reliable messages may be transmitted at a low, medium, or high priority.

**Reliable Unicast:** `courierClient.SendReliableAsync(recipientId, auditState);`  
**Unreliable Unicast:** `courierClient.SendUnreliableAsync(recipientId, auditState);`  
**Unreliable Broadcast:** `courierClient.SendBroadcastAsync(auditState);`

## Management Objects
Creating a Mob:
```csharp
[Guid("E6867903-3222-40ED-94BB-3C2C0FDB891B")]
public class TestMob {
  [ManagedProperty] public int Current { get; set; }
  [ManagedOperation] public int GetNext() => Current++;
  [ManagedOperation] public MessageDto GetMessage() => new MessageDto {
     Body = new List<object> { 1, 2, 3 },
     ReceiverId = Guid.NewGuid(),
     SenderId = Guid.NewGuid()
  };

  [ManagedOperation]
  public string SayHello(string name) => $"Hello, {name}!";
  
  [ManagedProperty(IsDataSource = true)]
  public int Sin => (int)(100 * Math.Sin(DateTime.Now.ToUnixTimeMilliseconds() * Math.PI * 2.0 / 30000) + 50);
}
```
Registering a Mob:
```csharp
var testMob = new TestMob();
courierClient.RegisterMob(testMob);
```
Connecting via DMI:
```
> use tcp 127.0.0.1:21337
> fetch-mobs
> tree
*
 * Dargon
  * Courier
   * TransportTier
    * Udp
     * Management
      * UdpDebugMob
    * Tcp
     * Management
      * TcpDebugMob
 * dummy_management_object_server
  * TestMob
> cd !!TestMob
> fetch-ops
> ls
GetNext (): Int32                Current
GetMessage (): MessageDto        Sin
SayHello (name: String): String  BoolSin
> set Current 10
> invoke GetNext
10
> graph Sin
                            dummy_management_object_server.TestMob.Sin
                                                                                                  │168.80
                                                                                                  │
         ▄▄▄               ▄▄               ▄▄▄              ▄▄▄               ▄▄              ▄▄▄│
         ▄  ▄             ▄  ▄             ▄  ▄              ▄                ▄ ▄                 │
            ▄            ▄                 ▄                ▄   ▄            ▄   ▄             ▄  │
        ▄                     ▄                ▄            ▄   ▄            ▄   ▄            ▄   │
        ▄    ▄           ▄    ▄           ▄                                                       │
                                               ▄           ▄     ▄          ▄     ▄           ▄   │
       ▄     ▄          ▄      ▄          ▄                                                       │
                                                ▄                ▄          ▄      ▄         ▄    │
      ▄       ▄         ▄                ▄                ▄                                       │
                               ▄                 ▄                ▄                ▄        ▄     │
                       ▄                 ▄                ▄                ▄                      │
      ▄        ▄                ▄                ▄                ▄                               │
                                        ▄                ▄                 ▄        ▄       ▄     │
     ▄         ▄       ▄        ▄                 ▄                                               │
                                                         ▄         ▄      ▄         ▄      ▄      │
     ▄          ▄     ▄          ▄     ▄                                                          │
                                       ▄          ▄     ▄           ▄    ▄           ▄     ▄      │
    ▄           ▄    ▄            ▄                ▄                                              │
▄                    ▄                ▄                ▄            ▄    ▄           ▄    ▄       │
    ▄            ▄                ▄                ▄                 ▄  ▄             ▄   ▄       │
▄  ▄             ▄  ▄              ▄  ▄             ▄  ▄             ▄                 ▄ ▄        │
 ▄▄               ▄▄▄              ▄▄▄               ▄▄               ▄▄▄              ▄▄         │
                                                                                                  │
                                                                                                  │-68.80
──────────────────────────────────────────────────────────────────────────────────────────────────┘
2.8m ago                                                                                406.9ms ago
^: 149.00, v: -49.00, µ: 48.08
```
## Aggregators
Creating an aggregator:
```
var outboundBytesAggregator = auditService.GetAggregator<double>(DataSetNames.kOutboundBytes);
```

Writing to an aggregator:
```
outboundBytesAggregator.Put(sizeof(int) + length);
```

Exposing an aggregator:
```
[ManagedDataSet("outbound_bytes", DataSetNames.kOutboundBytes, typeof(IAuditAggregator<double>))]
...
[Guid("2170CAA2-A8FF-40F2-84F9-ED648B83D0C7")]
public class TcpDebugMob {
   ...
}
```
Graphing from an aggregator:
```
> graph outbound_bytes
════════════════ Dargon.Courier.TransportTier.Tcp.Management.TcpDebugMob.outbound_bytes ═════════════════
           Min                                Max                                Sum
              ▄          │3478.00                ▄          │3478.00                ▄          │3478.00
             ▄           │                      ▄           │                      ▄           │
                         │                                  │                                  │
                         │                                  │                                  │
                         │                                  │                                  │
          ▄              │                   ▄              │                   ▄              │
                         │                                  │                                  │
                       ▄▄│                                ▄▄│                               ▄▄▄│
                   ▄▄    │                            ▄▄ ▄  │                            ▄▄    │
▄     ▄            ▄  ▄▄ │         ▄     ▄            ▄   ▄ │         ▄     ▄            ▄   ▄ │
           ▄             │-230.00             ▄             │-230.00             ▄             │-230.00
─────────────────────────┘         ─────────────────────────┘         ─────────────────────────┘
3.9m ago          3.2s ago         3.9m ago          3.2s ago         3.9m ago          3.2s ago
^: 3169.00, v: 79.00, µ: 1042.07   ^: 3169.00, v: 79.00, µ: 1064.33   ^: 3169.00, v: 79.00, µ: 1093.07

                 Average                                                Count
                        ▄                 │3478.00                                            ▄   │3.20
                      ▄▄                  │                                                       │
                                          │                                                       │
                                          │                                                       │
                                          │                                                       │
                 ▄                        │                                                       │
                                          │                                                       │
                                        ▄▄│                                                       │
                                  ▄       │                                                       │
▄         ▄                      ▄    ▄▄  │                                                       │
                   ▄                      │-230.00  ▄          ▄      ▄  ▄  ▄▄▄         ▄▄▄    ▄▄▄│0.80
──────────────────────────────────────────┘         ──────────────────────────────────────────────┘
3.9m ago                           3.2s ago         3.9m ago                               3.2s ago
^: 3169.00, v: 79.00, µ: 1053.60                    ^: 3.00, v: 1.00, µ: 1.13
```

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

This can be useful for testing message routes.

### Reading information of Remote Courier Node by Id
We probably could have published the audit info through Courier's node state. If we'd done so, reading the info would look as follows:
``` csharp
var remoteEndpoint = courierClient.GetRemoteCourierEndpointOrNull(peerId);
var remoteAddress = remoteEndpoint.LastAddress;
var remoteName = remoteEndpoint.Name;
var remoteAuditInfo = remoteEndpoint.GetProperty<AuditInfoDto>(kAuditInfoKey);
```
