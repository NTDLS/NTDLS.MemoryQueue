# NTDLS.MemoryQueue

**NTDLS.MemoryQueue** is an in-memory, non-persistent message queue designed for efficient inter-process communication, task queuing, load balancing, and data buffering over TCP/IP. A single `MqServer` process holds queues in memory and fans messages out to any number of connected `MqClient` processes; messages are serialized with JSON (Newtonsoft.Json) and delivered over a reliable TCP transport. Queues support configurable delivery schemes (round-robin or random), consumption schemes, throttling, batching, delivery retries, and message expiry.

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/NTDLS.MemoryQueue

## Why use it?

You may want **NTDLS.MemoryQueue** when you need fast, lightweight messaging between processes on one or more machines without bringing in a heavyweight broker such as RabbitMQ, Azure Service Bus, or Kafka:

- **Inter-process communication** — exchange typed .NET objects between separate applications (or between parts of the same application) over TCP/IP, with no shared files, named pipes, or database round-trips.
- **Task queuing & work distribution** — publish work items to a queue and let multiple worker processes subscribe and pick them up; the `RoundRobbin` delivery scheme spreads messages across subscribers automatically.
- **Load balancing** — distribute incoming requests or events across several consumer processes, with per-queue throttling (`DeliveryThrottle`) and batching (`BatchDeliveryInterval`) to protect consumers from bursts.
- **Buffering** — absorb spikes in production by letting a server buffer messages in memory while consumers catch up; `MaxMessageAge` drops stale items so old data never reaches a subscriber.
- **Low operational overhead** — everything runs in process memory. There is nothing to install, provision, or persist, and a full server can be started in two lines of code. The companion [NTDLS.MemoryQueueServer](https://github.com/NTDLS/NTDLS.MemoryQueue) project shows how to host the server as a Windows service with a web UI.

## Usage examples

All snippets below target the public API of the `NTDLS.MemoryQueue` package (`MqServer`, `MqClient`, `IMqMessage`, `MqQueueConfiguration`).

### 1. Define a message type

Any dispatched message must implement the marker interface `IMqMessage`:

```csharp
using NTDLS.MemoryQueue;

// A message is just a POCO implementing IMqMessage.
public class MyMessage(string text) : IMqMessage
{
    public string Text { get; set; } = text;
}
```

### 2. Start a queue server

The server needs only a listen port. It can run in its own dedicated process or inside any existing process:

```csharp
using NTDLS.MemoryQueue;

var server = new MqServer();

// Optionally log server-side problems (e.g. repeated failed deliveries).
server.OnException += (MqServer srv, MqQueueConfiguration? queue, Exception ex) =>
    Console.WriteLine($"Queue '{queue?.QueueName}' error: {ex.Message}");

// Listen for queue clients on TCP port 45784.
server.Start(45784);

Console.WriteLine("Press [enter] to shutdown.");
Console.ReadLine();

// Stop the server and all of its queues.
server.Stop();
```

### 3. Connect a client

Clients connect by host name or IP address and re-connect automatically if the connection drops (configurable via `MqClientConfiguration.AutoReconnect`):

```csharp
using NTDLS.MemoryQueue;

var client = new MqClient();

// Connect to the queue server.
client.Connect("127.0.0.1", 45784);
```

### 4. Create a queue and enqueue messages

Queues are created on the server through the client. You can use the defaults, or pass an `MqQueueConfiguration` to control delivery behaviour:

```csharp
using NTDLS.MemoryQueue;

// Simplest form: create "Jobs" with default settings.
client.CreateQueue("Jobs");

// Or configure the queue explicitly:
var config = new MqQueueConfiguration("Reports")
{
    // Deliver each message to exactly one subscriber, in turn.
    DeliveryScheme = MqClient.DeliveryScheme.RoundRobbin,
    // Remove the message once ANY subscriber consumes it.
    ConsumptionScheme = MqClient.ConsumptionScheme.FirstConsumedSubscriber,
    // Wait 100 ms between individual deliveries to smooth out bursts.
    DeliveryThrottle = TimeSpan.FromMilliseconds(100),
    // Drop any message older than 5 minutes.
    MaxMessageAge = TimeSpan.FromMinutes(5),
};
client.CreateQueue(config);

// Enqueue a typed message onto the queue.
client.Enqueue("Jobs", new MyMessage("Hello from the producer!"));
```

### 5. Subscribe and receive messages

Subscribing tells the server to push messages from that queue to this client. The handler returns `true` to mark the message as consumed:

```csharp
using NTDLS.MemoryQueue;

// Receive notifications for messages enqueued on "Jobs".
client.Subscribe("Jobs");

// Handle incoming messages; return true to mark the message as consumed.
client.OnReceived += (MqClient c, IMqMessage message) =>
{
    if (message is MyMessage myMessage)
    {
        Console.WriteLine($"Received: '{myMessage.Text}'");
    }
    return true;
};

// ... later, stop receiving from the queue ...
client.Unsubscribe("Jobs");
```

### 6. Manage queues and clean up

The client can also purge or delete queues, and disconnects cleanly when done:

```csharp
using NTDLS.MemoryQueue;

// Remove all pending messages from a queue.
client.PurgeQueue("Jobs");

// Delete the queue entirely.
client.DeleteQueue("Jobs");

// Disconnect from the server.
client.Disconnect();
```

## License

[MIT](https://choosealicense.com/licenses/mit/)
