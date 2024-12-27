using NTDLS.MemoryQueue.Management;
using NTDLS.MemoryQueue.Payloads.Queries.ServerToClient;
using NTDLS.MemoryQueue.Server;
using NTDLS.MemoryQueue.Server.QueryHandlers;
using NTDLS.ReliableMessaging;
using NTDLS.Semaphore;
using System.Collections.ObjectModel;
using System.Net;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Listens for connections from MessageClients and processes the incoming notifications/queries.
    /// </summary>
    public class MqServer
    {
        private readonly RmServer _rmServer;
        private readonly PessimisticCriticalResource<CaseInsensitiveMessageQueueDictionary> _messageQueues = new();
        private readonly MqServerConfiguration _configuration;

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

        /// <summary>
        /// Returns a read-only copy of the running configuration.
        /// </summary>
        /// <returns></returns>
        public MqServerInformation GetConfiguration()
        {
            return new MqServerInformation
            {
                AsynchronousQueryWaiting = _configuration.AsynchronousQueryWaiting,
                QueryTimeout = _configuration.QueryTimeout,
                InitialReceiveBufferSize = _configuration.InitialReceiveBufferSize,
                MaxReceiveBufferSize = _configuration.MaxReceiveBufferSize,
                ReceiveBufferGrowthRate = _configuration.ReceiveBufferGrowthRate,
                ListenPort = _rmServer.ListenPort
            };
        }

        /// <summary>
        /// Returns a read-only copy of the queues.
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<MqQueueInformation> GetQueues()
        {
            while (true)
            {
                bool success = false;
                List<MqQueueInformation>? result = new();

                success = _messageQueues.TryUse(mq =>
                {
                    foreach (var q in mq)
                    {
                        var enqueuedMessagesSuccess = q.Value.EnqueuedMessages.TryUse(m =>
                        {
                            result.Add(new MqQueueInformation
                            {
                                BatchDeliveryInterval = q.Value.QueueConfiguration.BatchDeliveryInterval,
                                ConsumptionScheme = q.Value.QueueConfiguration.ConsumptionScheme,
                                //CurrentEnqueuedMessageCount = m.Count,
                                DeliveryScheme = q.Value.QueueConfiguration.DeliveryScheme,
                                DeliveryThrottle = q.Value.QueueConfiguration.DeliveryThrottle,
                                MaxDeliveryAttempts = q.Value.QueueConfiguration.MaxDeliveryAttempts,
                                MaxMessageAge = q.Value.QueueConfiguration.MaxMessageAge,
                                QueueName = q.Value.QueueConfiguration.QueueName,
                                TotalDeliveredMessages = q.Value.TotalDeliveredMessages,
                                TotalEnqueuedMessages = q.Value.TotalEnqueuedMessages,
                                TotalExpiredMessages = q.Value.TotalExpiredMessages
                            });
                        });

                        if (!enqueuedMessagesSuccess)
                        {
                            //Failed to lock, break the inner loop and try again.
                            result = null;
                            break;
                        }
                    }
                });

                if (success && result != null)
                {
                    return new ReadOnlyCollection<MqQueueInformation>(result);
                }

                Thread.Sleep(1); //Failed to lock, sleep then try again.
            }
        }

        /// <summary>
        /// Returns a read-only copy of the queue subscribers.
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<MqSubscriberInformation> GetSubscribers(string queueName)
        {
            while (true)
            {
                bool success = false;
                var result = new List<MqSubscriberInformation>();

                success = _messageQueues.TryUse(mq =>
                {
                    if (mq.TryGetValue(queueName, out var messageQueue))
                    {
                        success = messageQueue.Subscribers.TryUse(s =>
                        {
                            foreach (var subscriber in s)
                            {
                                result.Add(subscriber.Value);
                            }
                        });
                    }
                    else
                    {
                        throw new Exception($"Queue not found: [{queueName}].");
                    }
                });

                if (success)
                {
                    return new ReadOnlyCollection<MqSubscriberInformation>(result);
                }

                Thread.Sleep(1); //Failed to lock, sleep then try again.
            }
        }

        /// <summary>
        /// Returns a read-only copy messages in the queue.
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<MqEnqueuedMessageInformation> GetQueueMessages(string queueName, int offset, int take)
        {
            while (true)
            {
                bool success = false;
                List<MqEnqueuedMessageInformation>? result = new();

                success = _messageQueues.TryUse(mq =>
                {
                    var filteredQueues = mq.Where(o => o.Value.QueueConfiguration.QueueName.Equals(queueName, StringComparison.OrdinalIgnoreCase));
                    foreach (var q in filteredQueues)
                    {
                        var enqueuedMessagesSuccess = q.Value.EnqueuedMessages.TryUse(m =>
                        {
                            foreach (var message in m.Skip(offset).Take(take))
                            {
                                result.Add(new MqEnqueuedMessageInformation
                                {
                                    Timestamp = message.Timestamp,
                                    SubscriberMessageDeliveries = message.SubscriberMessageDeliveries.Keys.ToHashSet(),
                                    SatisfiedSubscribersConnectionIDs = message.SatisfiedSubscribersConnectionIDs,
                                    ObjectType = message.ObjectType,
                                    MessageJson = message.MessageJson,
                                    MessageId = message.MessageId
                                });
                            }
                        });

                        if (!enqueuedMessagesSuccess)
                        {
                            //Failed to lock, break the inner loop and try again.
                            result = null;
                            break;
                        }
                    }
                });

                if (success && result != null)
                {
                    return new ReadOnlyCollection<MqEnqueuedMessageInformation>(result);
                }

                Thread.Sleep(1); //Failed to lock, sleep then try again.
            }
        }

        /// <summary>
        /// Returns a read-only copy messages in the queue.
        /// </summary>
        /// <returns></returns>
        public MqEnqueuedMessageInformation GetQueueMessage(string queueName, Guid messageId)
        {
            while (true)
            {
                bool success = false;
                MqEnqueuedMessageInformation? result = null;

                success = _messageQueues.TryUse(mq =>
                {
                    if (mq.TryGetValue(queueName, out var messageQueue))
                    {
                        success = messageQueue.EnqueuedMessages.TryUse(m =>
                        {
                            var message = m.Where(o => o.MessageId == messageId).FirstOrDefault();
                            if (message != null)
                            {
                                result = new MqEnqueuedMessageInformation
                                {
                                    Timestamp = message.Timestamp,
                                    SubscriberMessageDeliveries = message.SubscriberMessageDeliveries.Keys.ToHashSet(),
                                    SatisfiedSubscribersConnectionIDs = message.SatisfiedSubscribersConnectionIDs,
                                    ObjectType = message.ObjectType,
                                    MessageJson = message.MessageJson,
                                    MessageId = message.MessageId
                                };
                            }
                            else
                            {
                                throw new Exception($"Message not found: [{messageId}].");
                            }
                        });
                    }
                    else
                    {
                        throw new Exception($"Queue not found: [{queueName}].");
                    }
                });

                if (success && result != null)
                {
                    return result;
                }

                Thread.Sleep(1); //Failed to lock, sleep then try again.
            }
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

            _messageQueues.Use(o =>
            {
                //Stop all message queues.
                foreach (var q in o)
                {
                    q.Value.Stop();
                }
            });
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
        internal void SubscribeToQueue(Guid connectionId, IPEndPoint? localEndpoint, IPEndPoint? remoteEndpoint, string queueName)
        {
            _messageQueues.Use(o =>
            {
                string queueKey = queueName.ToLowerInvariant();
                if (o.TryGetValue(queueKey, out var messageQueue))
                {
                    messageQueue.Subscribers.Use(s =>
                    {
                        if (s.ContainsKey(connectionId) == false)
                        {
                            s.Add(connectionId, new MqSubscriberInformation(connectionId)
                            {
                                LocalAddress = localEndpoint?.Address?.ToString(),
                                RemoteAddress = remoteEndpoint?.Address?.ToString(),
                                LocalPort = localEndpoint?.Port,
                                RemotePort = remoteEndpoint?.Port
                            });
                        }
                    });
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
                    messageQueue.TotalEnqueuedMessages++;
                    messageQueue.EnqueuedMessages.Use(s => s.Add(new EnqueuedMessage(objectType, messageJson)));
                    messageQueue.DeliveryThreadWaitEvent.Set();
                }
                else
                {
                    throw new Exception($"Queue not found: [{queueName}].");
                }
            });
        }

        /// <summary>
        /// Removes all messages from the given queue.
        /// </summary>
        internal void PurgeQueue(string queueName)
        {
            _messageQueues.Use(o =>
            {
                string queueKey = queueName.ToLowerInvariant();
                if (o.TryGetValue(queueKey, out var messageQueue))
                {
                    messageQueue.EnqueuedMessages.Use(s => s.Clear());
                }
                else
                {
                    throw new Exception($"Queue not found: [{queueName}].");
                }
            });
        }

        #endregion
    }
}
