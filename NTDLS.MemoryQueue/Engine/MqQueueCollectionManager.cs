using System.Diagnostics.CodeAnalysis;

namespace NTDLS.MemoryQueue.Engine
{
    /// <summary>
    /// The collection manager for all of the queues.
    /// </summary>
    internal class MqQueueCollectionManager
    {
        /// <summary>
        /// The server that is using this queue manager.
        /// </summary>
        public MqServer? Server { get; private set; }

        /// <summary>
        /// The collection of queues.
        /// </summary>
        public List<MqQueue> Queues { get; private set; } = new();

        /// <summary>
        /// Since we are using a PessimisticCriticalResource, we cant use a parameterized constructor so we have to set the server instance later.
        /// </summary>
        /// <param name="server"></param>
        public void SetServer(MqServer server)
        {
            Server = server;
        }

        /// <summary>
        /// Shuts down all queues and the queue manager.
        /// </summary>
        /// <param name="waitForThreadToExit"></param>
        public void Shutdown(bool waitForThreadToExit)
        {
            foreach (var queue in Queues)
            {
                queue.Shutdown(waitForThreadToExit);
            }
        }

        /// <summary>
        /// Subscribes the specified connection the the specified queue name.
        /// </summary>
        public void Subscribe(Guid connectionId, string queueName)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.Subscribers.Add(connectionId);
        }

        /// <summary>
        /// Unsubscribes the specified connection the the specified queue name.
        /// </summary>
        public void Unsubscribe(Guid connectionId, string queueName)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.Subscribers.Remove(connectionId);
        }

        /// <summary>
        /// Enqueues a message to the specified queue.
        /// </summary>
        public void EnqueueMessage(string queueName, string payloadJson, string payloadType)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.AddMessage(payloadJson, payloadType);
        }

        /// <summary>
        /// Enqueues a query (which expects a reply) to the specified queue.
        /// </summary>
        public void EnqueueQuery(Guid originationId, string queueName, Guid queryId, string payloadJson, string payloadType, string replyType)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.AddQuery(originationId, queryId, payloadJson, payloadType, replyType);
        }

        /// <summary>
        /// Enqueues a query reply to the specified queue. This reply will be routed to the connection with the originationId.
        /// </summary>
        public void EnqueueQueryReply(Guid originationId, string queueName, Guid queryId, string payloadJson, string payloadType, string replyType)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.AddQueryReply(originationId, queryId, payloadJson, payloadType, replyType);
        }

        /// <summary>
        /// Creates a new queue.
        /// </summary>
        public void Create(MqQueueConfiguration config)
        {
            if (Exists(config.Name))
            {
                throw new Exception($"The queue already exists: {config.Name}.");
            }

            var queue = new MqQueue(this, config);
            Queues.Add(queue);
        }

        /// <summary>
        /// Deletes an existing queue.
        /// </summary>
        public void Delete(string queueName)
        {
            if (TryGet(queueName, out var queue))
            {
                queue.Shutdown(true);
                Queues.Remove(queue);
            }
        }

        /// <summary>
        /// Attempts to get a queue by its name.
        /// </summary>
        public bool TryGet(string key, [NotNullWhen(true)] out MqQueue? outQueue)
        {
            key = key.ToLower();
            outQueue = Queues.Where(o => o.Key == key).FirstOrDefault();
            return outQueue != null;
        }

        /// <summary>
        /// Determines if a queue with the specified name exists.
        /// </summary>
        public bool Exists(string key)
        {
            key = key.ToLower();
            return Queues.Any(o => o.Key == key);
        }
    }
}
