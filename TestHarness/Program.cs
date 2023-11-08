using NTDLS.MemoryQueue.Events;
using NTDLS.MemoryQueue.Payloads;
using NTDLS.ReliableMessaging;

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

            client.OnMessageReceived += (NmqClient client, NmqMessageReceivedEventParam message) =>
            {
                Console.WriteLine($"Client received message from server: {message.Payload}");
            };

            client.OnQueryReceived += (NmqClient client, INmqQuery query) =>
            {
                if (query is MyQuery myQuery)
                {
                    return new MyQueryReply("This is my reply");
                }

                throw new Exception("The query was unhandled.");
            };

            client.Connect("localhost", 45784);
            client.CreateQueue(new NmqQueueConfiguration("MyFirstQueue"));
            client.Subscribe("MyFirstQueue");

            client.Enqueue("MyFirstQueue", "This is my message");

            client.Query<MyQueryReply>("MyFirstQueue", new MyQuery("Ping!")).ContinueWith((o) =>
            {
                if (o.IsCompletedSuccessfully && o.Result != null)
                {
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

            /*
            //Start a server and add a "query received" and "notification received" event handler.
            server.OnQueryReceived += Server_OnQueryReceived;
            server.OnNotificationReceived += Server_OnNotificationReceived;
            server.Start(45784);

            //Start a client and connect to the server.
            var client = new MessageClient();
            client.Connect("localhost", 45784);

            client.Notify(new MyNotification("This is message 001 from the client."));
            client.Notify(new MyNotification("This is message 002 from the client."));
            client.Notify(new MyNotification("This is message 003 from the client."));

            //Send a query to the server, specify which type of reply we expect.
            client.Query<MyQueryReply>(new MyQuery("This is the query from the client.")).ContinueWith(x =>
            {
                //If we recevied a reply, print it to the console.
                if (x.IsCompletedSuccessfully && x.Result != null)
                {
                    Console.WriteLine($"Client received query reply: '{x.Result.Message}'");
                }
            });
            */
        }



        /*
        private static void Server_OnNotificationReceived(MessageServer server, Guid connectionId, IFrameNotification payload)
        {
            if (payload is MyNotification notification)
            {
                Console.WriteLine($"Server received notification: {notification.Message}");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static IFrameQueryReply Server_OnQueryReceived(MessageServer server, Guid connectionId, IFrameQuery payload)
        {
            if (payload is MyQuery query)
            {
                Console.WriteLine($"Server received query: '{query.Message}'");

                //Return with a class that implements IFrameQueryReply to reply to the client.
                return new MyQueryReply("This is the query reply from the server.");
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        */
    }
}