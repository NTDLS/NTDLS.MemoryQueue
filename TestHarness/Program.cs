using NTDLS.MemoryQueue;

namespace TestHarness
{
    internal class Program
    {
        internal class MyMessage(string text) : IMqMessage
        {
            public string Text { get; set; } = text;
        }

        static void Main()
        {
            var server = new MqServer();
            server.Start(45784);

            var client = new MqClient();
            client.Connect("127.0.0.1", 45784);
            client.CreateQueue("MyFirstQueue");
            client.Subscribe("MyFirstQueue");
            client.OnReceived += Client_OnReceived;

            for(int i = 0; i < 10; i++)//Send test messages as objects that inherit from IMqMessage
            {
                client.Enqueue("MyFirstQueue", new MyMessage($"Test message {i:n0}"));
            }

            Console.WriteLine("Press [enter] to shutdown.");
            Console.ReadLine();

            //Cleanup.
            client.Disconnect();
            server.Stop();
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