﻿using NTDLS.MemoryQueue.Payloads;
using NTDLS.ReliableMessaging;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.MemoryQueue.Engine
{
    internal class NmqQueues
    {
        public NmqServer? Server { get; private set; }

        public List<NmqQueue> Collection { get; private set; } = new();

        public void SetServer(NmqServer server)
        {
            Server = server;
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

        public void Equeue(Guid connectionId, string queueName, string payload)
        {
            if (TryGet(queueName, out var queue) == false)
            {
                throw new Exception($"The queue does not exists: {queueName}.");
            }

            queue.AddMessage(payload);
        }

        public void Add(NmqQueueConfiguration config)
        {
            if (ContainsKey(config.Name))
            {
                throw new Exception($"The queue already exists: {config.Name}.");
            }

            var queue = new NmqQueue(this, config);
            Collection.Add(queue);
        }

        public bool TryGet(string key, [NotNullWhen(true)] out NmqQueue? outQueu)
        {
            key = key.ToLower();
            outQueu = Collection.Where(o => o.Key == key).FirstOrDefault();
            return outQueu != null;
        }

        public bool ContainsKey(string key)
        {
            key = key.ToLower();
            return Collection.Any(o => o.Key == key);
        }
    }
}
