using NTDLS.MemoryQueue;

namespace TestHarness
{
    internal class Program
    {
        internal class MyMessage : INmqMessage
        {
            public string Text { get; set; }

            public MyMessage(string text)
            {
                Text = text;
            }
        }

        internal class MyQuery : INmqQuery
        {
            public string Message { get; set; }

            public MyQuery(string message)
            {
                Message = message;
            }
        }

        internal class MyQueryReply : INmqQueryReply
        {
            public string Message { get; set; }

            public MyQueryReply(string message)
            {
                Message = message;
            }
        }

        static NmqServer PropupServer()
        {
            var server = new NmqServer();
            //server.CreateQueue(new NmqConfiguration("MyFirstQueue"));
            server.Start(45784);

            return server;
        }

        static NmqClient PropupClient()
        {
            //Start a client and connect to the server.
            var client = new NmqClient();

            //Add a message receipt handler.
            client.OnMessageReceived += (NmqClient client, INmqMessage message) =>
            {
                if (message is MyMessage myMessage)
                {
                    Console.WriteLine($"Client received message from server: {myMessage.Text}");
                }
            };

            //Add a query receipt handler.
            client.OnQueryReceived += (NmqClient client, INmqQuery query) =>
            {
                //Handle a query of type MyQuery. Return with a type MyQueryReply.
                if (query is MyQuery myQuery)
                {
                    return new MyQueryReply("This is my reply");
                }

                throw new Exception("The query was unhandled.");
            };


            client.Connect("localhost", 45784); //Connect to the queue server.
            client.CreateQueue(new NmqQueueConfiguration("MyFirstQueue")); //Create a queue.
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