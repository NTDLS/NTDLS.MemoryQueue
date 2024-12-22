﻿using NTDLS.MemoryQueue.Payloads.Queries.ServerToClient;
using NTDLS.MemoryQueue.Server;
using NTDLS.MemoryQueue.Server.QueryHandlers;
using NTDLS.ReliableMessaging;
using NTDLS.Semaphore;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Listens for connections from MessageClients and processes the incoming notifications/queries.
    /// </summary>
    public class MqServer
    {
        private readonly RmServer _rmServer;
        private readonly PessimisticCriticalResource<Dictionary<string, MessageQueue>> _messageQueues = new();
        private MqServerConfiguration _configuration;

        /// <summary>
        /// Delegate used to notify of queue server exceptions.
        /// </summary>
        public delegate void OnExceptionEvent(MqServer server, MqQueueConfiguration? queue, Exception ex);

        /// <summary>
        /// Event used to notify of queue server exceptions.
        /// </summary>
        public event OnExceptionEvent? OnException;

        /// <summary>
        /// Creates a new instance of the queue service.
        /// </summary>
        public MqServer()
        {
            _configuration = new MqServerConfiguration();
            _rmServer = new RmServer();
            _rmServer.AddHandler(new InternalServerQueryHandlers(this));
            _rmServer.OnDisconnected += RmServer_OnDisconnected;
        }

        /// <summary>
        /// Creates a new instance of the queue service.
        /// </summary>
        public MqServer(MqServerConfiguration configuration)
        {
            _configuration = configuration;

            var rmConfiguration = new RmConfiguration()
            {
                AsynchronousQueryWaiting = configuration.AsynchronousQueryWaiting,
                InitialReceiveBufferSize = configuration.InitialReceiveBufferSize,
                MaxReceiveBufferSize = configuration.MaxReceiveBufferSize,
                QueryTimeout = configuration.QueryTimeout,
                ReceiveBufferGrowthRate = configuration.ReceiveBufferGrowthRate
            };

            _rmServer = new RmServer(rmConfiguration);
            _rmServer.AddHandler(new InternalServerQueryHandlers(this));
            _rmServer.OnDisconnected += RmServer_OnDisconnected;
        }

        internal void InvokeOnException(MqServer server, MqQueueConfiguration? queue, Exception ex)
            => OnException?.Invoke(server, queue, ex);

        private void RmServer_OnDisconnected(RmContext context)
        {
            //When a client disconnects, remove their subscriptions.
            _messageQueues.Use(mq =>
            {
                foreach (var q in mq)
                {
                    q.Value.Subscribers.Use(s => s.Remove(context.ConnectionId));
                }
            });
        }

        /// <summary>
        /// Starts the message queue server.
        /// </summary>
        public void Start(int listenPort)
        {
            _rmServer.Start(listenPort);
        }

        /// <summary>
        /// Stops the message queue server.
        /// </summary>
        public void Stop()
        {
            _rmServer.Stop();
        }

        /// <summary>
        /// Deliver a message from a server queue to a subscribed client.
        /// </summary>
        internal bool DeliverMessage(Guid connectionId, string queueName, EnqueuedMessage enqueuedMessage)
        {
            var result = _rmServer.Query(connectionId, new MessageDeliveryQuery(queueName, enqueuedMessage.ObjectType, enqueuedMessage.MessageJson)).Result;
            if (string.IsNullOrEmpty(result.ErrorMessage) == false)
            {
                throw new Exception(result.ErrorMessage);
            }
            return result.WasMessageConsumed;
        }

        #region Client Instructions.

        /// <summary>
        /// Creates a new empty queue if it does not already exist.
        /// </summary>
        internal void CreateQueue(MqQueueConfiguration queueConfiguration)
        {
            _messageQueues.Use(o =>
            {
                string queueKey = queueConfiguration.QueueName.ToLowerInvariant();
                if (o.ContainsKey(queueKey) == false)
                {
                    var messageQueue = new MessageQueue(this, queueConfiguration);
                    messageQueue.Start();
                    o.Add(queueKey, messageQueue);
                }
            });
        }

        /// <summary>
        /// Creates a new empty queue if it does not already exist.
        /// </summary>
        internal void DeleteQueue(string queueName)
        {
            _messageQueues.Use(o =>
            {
                string queueKey = queueName.ToLowerInvariant();
                if (o.TryGetValue(queueKey, out var messageQueue))
                {
                    messageQueue.Stop();

                    messageQueue.EnqueuedMessages.UseAll([messageQueue.Subscribers], d => o.Remove(queueKey));
                }
            });
        }

        /// <summary>
        /// Creates a subscription to a queue for a given connection id.
        /// </summary>
        internal void SubscribeToQueue(Guid connectionId, string queueName)
        {
            _messageQueues.Use(o =>
            {
                string queueKey = queueName.ToLowerInvariant();
                if (o.TryGetValue(queueKey, out var messageQueue))
                {
                    messageQueue.Subscribers.Use(s => s.Add(connectionId));
                }
            });
        }

        /// <summary>
        /// Removes a subscription from a queue for a given connection id.
        /// </summary>
        internal void UnsubscribeFromQueue(Guid connectionId, string queueName)
        {
            _messageQueues.Use(o =>
            {
                string queueKey = queueName.ToLowerInvariant();
                if (o.TryGetValue(queueKey, out var messageQueue))
                {
                    messageQueue.Subscribers.Use(s => s.Remove(connectionId));
                }
            });
        }

        /// <summary>
        /// Removes a subscription from a queue for a given connection id.
        /// </summary>
        internal void EnqueueMessage(string queueName, string objectType, string messageJson)
        {
            _messageQueues.Use(o =>
            {
                string queueKey = queueName.ToLowerInvariant();
                if (o.TryGetValue(queueKey, out var messageQueue))
                {
                    messageQueue.EnqueuedMessages.Use(s => s.Add(new EnqueuedMessage(objectType, messageJson)));
                    messageQueue.DeliveryThreadWaitEvent.Set();
                }
            });
        }

        #endregion
    }
}
