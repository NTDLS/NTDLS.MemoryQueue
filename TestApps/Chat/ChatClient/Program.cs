using ChatLibrary;
using NTDLS.MemoryQueue;

namespace ChatClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var client = new MqClient();

            //client.Connect("localhost", 45784);

            string queueName = "TestApps.Chat";

            client.OnMessageReceived += Client_OnMessageReceived;
            client.CreateQueue(new MqQueueConfiguration(queueName));
            client.Subscribe(queueName);

            Console.WriteLine("Chat client connected. Type /bye to close.");

            while (true)
            {
                var message = Console.ReadLine();
                if (message == "/bye")
                {
                    break;
                }

                client.EnqueueMessage("queueName", new ChatMessage(message));
            }

            client.Disconnect();
        }

        private static void Client_OnMessageReceived(MqClient client, IMqMessage message)
        {
            if (message is ChatMessage chatMessage)
            {
                Console.WriteLine($"Received: {chatMessage.Text}");
            }
        }
    }
}