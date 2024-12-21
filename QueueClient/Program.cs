﻿using NTDLS.MemoryQueue;

namespace QueueClient
{
    internal class Program
    {
        internal class MyMessage(string text) : IMqMessage
        {
            public string Text { get; set; } = text;
        }

        static readonly string[] _allQueueNames =
           ["QueueA", "QueueB", "QueueC", "QueueD", "QueueE",
            "QueueF", "QueueG", "QueueH", "QueueI", "QueueJ",
            "QueueK", "QueueL", "QueueM", "QueueN", "QueueO",
            "QueueP", "QueueQ", "QueueR", "QueueS", "QueueT",
            "QueueU", "QueueV", "QueueW", "QueueX", "QueueY",
            "QueueZ", "Queue1", "Queue2", "Queue3", "Queue4",
            "Queue5", "Queue6", "Queue7", "Queue8", "Queue9"];

        static void Main()
        {
            var random = new Random();

            var client = new MqClient();
            client.Connect("127.0.0.1", 45784);

            var myQueueNames = new HashSet<string>();

            int numberOfQueuesToCreate = random.Next(3, 10); //May create less depending on whether we push duplicates to the HashSet.
            for (int i = 0; i < numberOfQueuesToCreate; i++)
            {
                var queueName = _allQueueNames[random.Next(0, _allQueueNames.Length - 1)];
                myQueueNames.Add(queueName);

                Console.WriteLine($"Creating queue: '{queueName}'.");
                client.CreateQueue(queueName);

                if (random.Next(1, 100) > 50) //We don't always subscribe to our own queue.
                {
                    Console.WriteLine($"Subscribing to queue: '{queueName}'.");
                    client.Subscribe(queueName);
                }
            }

            client.OnReceived += Client_OnReceived;

            int clientId = Math.Abs(Guid.NewGuid().GetHashCode());

            int messageNumber = 0;
            while (messageNumber < 5000) //Send test messages as objects that inherit from IMqMessage
            {
                foreach (var queueName in myQueueNames)
                {
                    client.Enqueue(queueName, new MyMessage($"Test message {messageNumber++:n0} from {clientId}"));
                }
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
