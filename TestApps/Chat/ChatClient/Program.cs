using ChatLibrary;
using NTDLS.MemoryQueue;

namespace ChatClient
{
    internal class Program
    {
        static Guid _clientId = Guid.NewGuid();

        static void Main(string[] args)
        {
            var client = new MqClient();

            try
            {
                client.Connect("localhost", 45784);

                string queueName = "TestApps.Chat";

                client.OnMessageReceived += Client_OnMessageReceived;
                client.OnLog += (IMqMemoryQueue client, MqLogEntry entry) =>
                {
                    Console.WriteLine($"{entry.Severity} {entry.Message}");
                };

                try
                {
                    client.CreateQueue(new MqQueueConfiguration(queueName));
                }
                catch
                {
                }
                client.Subscribe(queueName);

                Console.WriteLine("Chat client connected. Type /bye to close.");

                while (true)
                {
                    var message = Console.ReadLine();
                    if (message == "/bye")
                    {
                        break;
                    }

                    client.EnqueueMessage(queueName, new ChatMessage(_clientId, message));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
            finally
            {
                client.Disconnect();
            }
        }
        private static void Client_OnMessageReceived(MqClient client, IMqMessage message)
        {
            if (message is ChatMessage chatMessage)
            {
                if (chatMessage.ClientId != _clientId)
                {
                    Console.WriteLine($"Received: {chatMessage.Text}");
                }
            }
        }
    }
}