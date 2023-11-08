using NTDLS.MemoryQueue.Payloads;
using NTDLS.ReliableMessaging;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.MemoryQueue.Engine
{
    internal class NmqQueueManager
    {
        public NmqServer? Server { get; private set; }
        public List<NmqQueue> Queues { get; private set; } = new();

        public void SetServer(NmqServer server)
        {
            Server = server;
        }

        public void Shutdown(bool waitForThreadToExit)
        {
            foreach (var queue in Queues)
            {
                queue.Shutdown(waitForThreadToExit);
            }
        }

        public void Subscribe(Guid connectionId, string queueName)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.Subscribers.Add(connectionId);
        }

        public void Unsubscribe(Guid connectionId, string queueName)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.Subscribers.Remove(connectionId);
        }

        public void Equeue(string queueName, string payload)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.AddMessage(payload);
        }

        public void EqueueQuery(Guid originationId, string queueName, Guid queryId, string payload)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.AddQuery(originationId, queryId, payload);
        }

        public void EqueueQueryReply(Guid originationId, string queueName, Guid queryId, string payload)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.AddQueryReply(originationId, queryId, payload);
        }

        public void Add(NmqQueueConfiguration config)
        {
            if (ContainsKey(config.Name))
            {
                throw new Exception($"The queue already exists: {config.Name}.");
            }

            var queue = new NmqQueue(this, config);
            Queues.Add(queue);
        }

        public void Delete(Guid connectionId, string queueName)
        {
            if (TryGet(queueName, out var queue))
            {
                queue.Shutdown(true);
                Queues.Remove(queue);
            }
        }

        public bool TryGet(string key, [NotNullWhen(true)] out NmqQueue? outQueu)
        {
            key = key.ToLower();
            outQueu = Queues.Where(o => o.Key == key).FirstOrDefault();
            return outQueu != null;
        }

        public bool ContainsKey(string key)
        {
            key = key.ToLower();
            return Queues.Any(o => o.Key == key);
        }
    }
}
