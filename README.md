# NTDLS.MemoryQueue

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/NTDLS.MemoryQueue

In memory non-persistent message queue with notifications/messages, query/reply
    support and several message broadcast schemes. Intended for inter-process-communication,
    queuing, load-balancing and buffering over TCP/IP.


>**Running the server:**
>
>Running the server can literally be done with two lines of code and can be run in the same process as the client.
>The server does not have to be dedicated either, it can be one of the process that is involved in inner-process-communication.
```csharp
internal class Program
{
    static var _server = new MqServer(); //Literal line #1.

    static void Main()
    {
        _server.Start(45784); //Literal line #2.

        Console.WriteLine("Press [enter] to shutdown.");
        Console.ReadLine();

        server.Shutdown();
    }
}
```


>**Enqueuing a message which does not expect a reply.**
>
>Enqueuing a one-way message that is broadcast to all connected peers that have subscribed to the queue.
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
        client.EnqueueMessage("MyFirstQueue", new MyMessage("This is a message"));

        Console.WriteLine("Press [enter] to shutdown.");
        Console.ReadLine();

        client.Disconnect();
    }
}
```


>**Receiving a notification message:**
>
>Receiving a notification is easy. If you are subscribed to the queue, you will receive the message.
>Further messages will be held until the event method returns.
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
        var client = new MqClient();

        //Connect to the queue server.
        client.Connect("localhost", 45784);

        //Wire up the OnMessageReceived event.
        client.OnMessageReceived += Client_OnMessageReceived;

        //Create a queue, its ok its it already exists.
        client.CreateQueue(new MqQueueConfiguration("MyFirstQueue"));

        //Subscribe to the queue.
        client.Subscribe("MyFirstQueue");

        Console.WriteLine("Press [enter] to shutdown.");
        Console.ReadLine();

        client.Disconnect();
    }

    private static void Client_OnMessageReceived(MqClient client, IMqMessage message)
    {
        //We received a message. There are numerous ways to handle these, but here
        //  we are going to pattern matching to determine what message was received.
        if (message is MyMessage myMessage)
        {
            Console.WriteLine($"Client received message from server: {myMessage.Text}");
        }
    }
}
```


>**Enqueuing a query, a message type that does expect a reply:**
>
>You can also enqueue a query. The query will be received by all subscribed clients and any one of them can respond.
> The async task will completed once a client receives and responds to the query.
> Note that only one clients reply will be received. All other replies are discarded.
```csharp
internal class Program
{
    internal class MyQuery : IMqQuery
    {
        public string Message { get; set; }

        public MyQuery(string message)
        {
            Message = message;
        }
    }

    internal class MyQueryReply : IMqQueryReply
    {
        public string Message { get; set; }

        public MyQueryReply(string message)
        {
            Message = message;
        }
    }

    static void Main()
    {
        var client = new MqClient();

        //Connect to the queue server.
        client.Connect("localhost", 45784);

        //Create a queue, its ok its it already exists.
        client.CreateQueue(new MqQueueConfiguration("MyFirstQueue"));

        //Subscribing to the queue is commented out to demonstrate that a client does not
        //  have to be subscribed to a queue to receive a reply from a query sent to that queue.
        //client.Subscribe("MyFirstQueue");

        //Enqueue a query that is to be distributed to all subscribers. The first one to reply wins.
        client.EnqueueQuery<MyQueryReply>("MyFirstQueue", new MyQuery("Ping!")).ContinueWith((o) =>
        {
            //We received a reply to the query. There are numerous ways to handle these,
            //  but here we are going to pattern matching to determine what message was received.
            if (o.IsCompletedSuccessfully && o.Result != null)
            {
                //We received a reply!
                Console.WriteLine($"Query Reply: {o.Result.Message}");
            }
        });

        Console.WriteLine("Press [enter] to shutdown.");
        Console.ReadLine();

        client.Disconnect();
    }
}
```


>**Receiving a query and replying to it:**
>
>Receiving a query and responding to it is easy. The server handles all the routing.
```csharp

internal class Program
{
    static void Main()
    {
        var client = new MqClient();

        //Connect to the queue server.
        client.Connect("localhost", 45784);

        //Wire up the OnQueryReceived event.
        client.OnQueryReceived += Client_OnQueryReceived;

        //Create a queue, its ok its it already exists.
        client.CreateQueue(new MqQueueConfiguration("MyFirstQueue"));

        //Subscribe to the queue.
        client.Subscribe("MyFirstQueue");

        Console.WriteLine("Press [enter] to shutdown.");
        Console.ReadLine();

        client.Disconnect();
    }

    private static IMqQueryReply Client_OnQueryReceived(MqClient client, IMqQuery query)
    {
        //We received a query. There are numerous ways to handle these, but here we are
        //  going to pattern matching to determine what query was received.
        if (query is MyQuery myQuery)
        {
            //The query of type MyQuery expects a reply of type MyQueryReply.
            return new MyQueryReply("This is my reply");
        }

        throw new Exception("The query was unhandled.");
    }
}
```

## License
[MIT](https://choosealicense.com/licenses/mit/)
