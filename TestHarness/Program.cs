using NTDLS.MemoryQueue;

namespace TestHarness
{
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

        static MqServer PropupServer()
        {
            var server = new MqServer();
            //server.CreateQueue(new NmqConfiguration("MyFirstQueue"));
            server.Start(45784);

            return server;
        }

        static MqClient PropupClient()
        {
            //Start a client and connect to the server.
            var client = new MqClient();

            //Add a message receipt handler.
            client.OnMessageReceived += (MqClient client, IMqMessage message) =>
            {
                if (message is MyMessage myMessage)
                {
                    Console.WriteLine($"Client received message from server: {myMessage.Text}");
                }
            };

            //Add a query receipt handler.
            client.OnQueryReceived += (MqClient client, IMqQuery query) =>
            {
                //Handle a query of type MyQuery. Return with a type MyQueryReply.
                if (query is MyQuery myQuery)
                {
                    return new MyQueryReply("This is my reply");
                }

                throw new Exception("The query was unhandled.");
            };


            client.Connect("localhost", 45784); //Connect to the queue server.
            client.CreateQueue(new MqQueueConfiguration("MyFirstQueue")); //Create a queue.
            client.Subscribe("MyFirstQueue"); //Subscribe to the queue.

            //Enqueue a one way message to be distributed to all subscribers.
            client.EnqueueMessage("MyFirstQueue", new MyMessage("This is a message"));

            //Enqueu a query that is to be distributed to all subscribers. The first one to reply wins.
            client.EnqueueQuery<MyQueryReply>("MyFirstQueue", new MyQuery("Ping!")).ContinueWith((o) =>
            {
                if (o.IsCompletedSuccessfully && o.Result != null)
                {
                    //We received a reply!
                    Console.WriteLine($"Query Reply: {o.Result.Message}");
                }
            });


            return client;
        }

        static void Main()
        {
            var server = PropupServer();
            var client = PropupClient();

            Console.WriteLine("Press [enter] to shutdown.");
            Console.ReadLine();

            //CLeanup.
            client.Disconnect();
            server.Shutdown();
        }
    }
}