using NTDLS.MemoryQueue;

namespace QueueClient
{
    internal class Program
    {
        internal class MyMessage(string text) : IMqMessage
        {
            public string Text { get; set; } = text;
        }

        static void Main()
        {
            var client = new MqClient();
            client.Connect("127.0.0.1", 45784);
            client.CreateQueue("MyFirstQueue");
            client.Subscribe("MyFirstQueue");
            client.OnReceived += Client_OnReceived;

            int clientId = Math.Abs(Guid.NewGuid().GetHashCode());

            int messageNumber = 0;
            while (messageNumber < 5000) //Send test messages as objects that inherit from IMqMessage
            {
                client.Enqueue("MyFirstQueue", new MyMessage($"Test message {messageNumber++:n0} from {clientId}"));
                //Thread.Sleep(10);
            }

            Console.WriteLine("Press [enter] to shutdown.");
            Console.ReadLine();

            //Cleanup.
            client.Disconnect();
        }

        private static bool Client_OnReceived(MqClient client, IMqMessage message)
        {
            if (message is MyMessage myMessage)
            {
                Console.WriteLine($"Received: '{myMessage.Text}'");
            }
            else
            {
                Console.WriteLine($"Received unknown message type.");
            }
            return true;
        }
    }
}
