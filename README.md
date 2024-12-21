# NTDLS.MemoryQueue

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/NTDLS.MemoryQueue

In memory non-persistent message queue intended for inter-process-communication,
    queuing, load-balancing and buffering over TCP/IP.


## Running the server:

Running the server can literally be done with two lines of code and can be run in the same process as the client.
The server does not have to be dedicated either, it can be one of the process that is involved in inner-process-communication.

```csharp
internal class Program
{
    static var _server = new MqServer(); //Literal line #1.

    static void Main()
    {
        _server.Start(45784); //Literal line #2.

        Console.WriteLine("Press [enter] to shutdown.");
        Console.ReadLine();

        server.Stop();
    }
}
```


## Enqueuing a message.
Enqueuing a one-way message that is broadcast to all connected peers that have subscribed to the queue.

```csharp
internal class Program
{
    internal class MyMessage : IMqMessage
    {
        public string Text { get; set; }

        public MyMessage(string text)
        {
            Text = text;
        }
    }

    static void Main()
    {
        //Start a client and connect to the server.
        var client = new MqClient();

        //Enqueue a one-way message to be distributed to all subscribers.
        client.Enqueue("MyFirstQueue", new MyMessage("This is a message"));

        Console.WriteLine("Press [enter] to shutdown.");
        Console.ReadLine();

        client.Disconnect();
    }
}
```

## Receiving a notification message
Receiving a notification is easy. If you are subscribed to the queue, you will receive the message.
Further messages will be held until the event method returns.

```csharp
internal class Program
{
    internal class MyMessage(string text): IMqMessage
    {
        public string Text { get; set; } = text;
    }

    static void Main()
    {
        var client = new MqClient();

        //Connect to the queue server.
        client.Connect("localhost", 45784);

        //Wire up the OnMessageReceived event.
        client.OnMessageReceived += Client_OnMessageReceived;

        //Create a queue, its ok its it already exists.
        client.CreateQueue("MyFirstQueue");

        //Subscribe to the queue.
        client.Subscribe("MyFirstQueue");

        Console.WriteLine("Press [enter] to shutdown.");
        Console.ReadLine();

        client.Disconnect();
    }

    private static bool Client_OnReceived(MqClient client, IMqMessage message)
    {
        //We received a message. Use pattern matching to determine what message was received.
        if (message is MyMessage myMessage)
        {
            Console.WriteLine($"Client received message from server: {myMessage.Text}");
        }

        return true; //Return true to tell the server we received and processed the message.
    }
}
```

## License
[MIT](https://choosealicense.com/licenses/mit/)
