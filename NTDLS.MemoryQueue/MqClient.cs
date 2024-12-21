using Newtonsoft.Json;
using NTDLS.MemoryQueue.Client.QueryHandlers;
using NTDLS.MemoryQueue.Payloads.Queries.ClientToServer;
using NTDLS.ReliableMessaging;
using System.Net;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Connects to a MessageServer then sends/received and processes notifications/queries.
    /// </summary>
    public class MqClient
    {
        private readonly RmClient _rmClient;

        /// <summary>
        /// Delegate used for server-to-client delivery notifications.
        /// </summary>
        /// <returns>Return true to mark the message as consumed by the client.</returns>
        public delegate bool OnReceivedEvent(MqClient client, IMqMessage message);

        /// <summary>
        /// Delegate used for server-to-client delivery notifications.
        /// </summary>
        /// <returns>Return true to mark the message as consumed by the client.</returns>
        public event OnReceivedEvent? OnReceived;

        /// <summary>
        /// Creates a new instance of the queue client.
        /// </summary>
        public MqClient()
        {
            var rmConfiguration = new RmConfiguration()
            {
                //TODO: implement some settings.
            };

            _rmClient = new RmClient(rmConfiguration);
            _rmClient.AddHandler(new InternalClientQueryHandlers(this));
        }

        internal bool InvokeOnReceived(MqClient client, IMqMessage message)
            => (OnReceived?.Invoke(client, message) == true);

        /// <summary>
        /// Connects the client to a queue server.
        /// </summary>
        public void Connect(string hostName, int port)
            => _rmClient.Connect(hostName, port);

        /// <summary>
        /// Connects the client to a queue server.
        /// </summary>
        public void Connect(IPAddress ipAddress, int port)
            => _rmClient.Connect(ipAddress, port);

        /// <summary>
        /// Disconnects the client from the queue server.
        /// </summary>
        public void Disconnect(bool wait = false)
            => _rmClient.Disconnect(wait);

        /// <summary>
        /// Instructs the server to create a queue with the given name.
        /// </summary>
        public void CreateQueue(string queueName)
        {
            var result = _rmClient.Query(new CreateQueueQuery(queueName)).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        /// <summary>
        /// Instructs the server delete the queue with the given name.
        /// </summary>
        public void DeleteQueue(string queueName)
        {
            var result = _rmClient.Query(new DeleteQueueQuery(queueName)).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        /// <summary>
        /// Instructs the server to notify the client of messages sent to the given queue.
        /// </summary>
        public void Subscribe(string queueName)
        {
            var result = _rmClient.Query(new SubscribeToQueueQuery(queueName)).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        /// <summary>
        /// Instructs the server to stop notifying the client of messages sent to the given queue.
        /// </summary>
        public void Unsubscribe(string queueName)
        {
            var result = _rmClient.Query(new UnsubscribeFromQueueQuery(queueName)).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        /// <summary>
        /// Dispatches a message to the queue server to be enqueued in the given queue.
        /// </summary>
        public void Enqueue<T>(string queueName, T message)
            where T : IMqMessage
        {
            var messageJson = JsonConvert.SerializeObject(message);

            var objectType = message.GetType()?.AssemblyQualifiedName ?? string.Empty;

            var result = _rmClient.Query(new EnqueueMessageToQueue(queueName, objectType, messageJson)).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }
    }
}
