using NTDLS.Helpers;
using NTDLS.Semaphore;

namespace NTDLS.MemoryQueue.Server
{
    /// <summary>
    /// A named message queue and its delivery thread.
    /// </summary>
    internal class MessageQueue(MqServer mqServer, MqQueueConfiguration queueConfiguration)
    {
        internal AutoResetEvent DeliveryThreadWaitEvent = new(false);

        private readonly MqServer QueueServer = mqServer;
        public bool KeepRunning { get; set; } = true;

        /// <summary>
        /// List of subscriber connection IDs.
        /// </summary>
        public PessimisticCriticalResource<HashSet<Guid>> Subscribers { get; set; } = new();

        /// <summary>
        /// Messages that are enqueued in this message queue.
        /// </summary>
        public PessimisticCriticalResource<List<EnqueuedMessage>> EnqueuedMessages { get; set; } = new();
        public MqQueueConfiguration QueueConfiguration { get; private set; } = queueConfiguration;
        public Thread DeliveryThread = new Thread(DeliveryThreadProc);

        private static void DeliveryThreadProc(object? pMessageQueue)
        {
            var messageQueue = pMessageQueue.EnsureNotNull<MessageQueue>();

            while (messageQueue.KeepRunning)
            {
                int successfulDeliveries = 0;

                try
                {
                    EnqueuedMessage? topMessage = null;
                    List<Guid>? yetToBeDeliveredSubscribers = null;

                    messageQueue.EnqueuedMessages.UseAll([messageQueue.Subscribers], m =>
                    {
                        messageQueue.Subscribers.Use(s =>
                        {
                            //We only process a queue if it has subscribers, this is so we do not discard messages for queues with no subscribers.
                            if (s.Count > 0)
                            {
                                topMessage = m.FirstOrDefault(); //Get the first message in the list, if any.

                                if (topMessage != null)
                                {
                                    //Get list of subscribers that have yet to get a copy of the message.
                                    yetToBeDeliveredSubscribers = s.Except(topMessage.DeliveredSubscriberConnectionIDs).ToList();
                                }
                            }
                        });
                    });

                    //We we have a message, deliver it to the queue subscribers.
                    if (topMessage != null && yetToBeDeliveredSubscribers != null)
                    {
                        foreach (var subscriberId in yetToBeDeliveredSubscribers)
                        {
                            if (messageQueue.QueueServer.DeliverMessage(subscriberId, messageQueue.QueueConfiguration.Name, topMessage))
                            {
                                //This thread is the only place we manage [SentToSubscriber], so we can use it without additional locking.
                                topMessage.DeliveredSubscriberConnectionIDs.Add(subscriberId);
                                successfulDeliveries++;
                            }
                        }

                        messageQueue.EnqueuedMessages.UseAll([messageQueue.Subscribers], m =>
                        {
                            messageQueue.Subscribers.Use(s =>
                            {
                                //If the message has been delivered to all subscribers, then remove the message.
                                if (s.Except(topMessage.DeliveredSubscriberConnectionIDs).Any() == false)
                                {
                                    //If the message has been delivered to all subscribers, then remove it from the message list.
                                    m.Remove(topMessage);
                                }
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    //TODO handle delivery exceptions.
                }

                if (successfulDeliveries == 0)
                {
                    messageQueue.DeliveryThreadWaitEvent.WaitOne(10);
                }
            }
        }

        public void Start()
        {
            KeepRunning = true;
            DeliveryThread.Start(this);
        }

        public void Stop()
        {
            KeepRunning = false;
            DeliveryThread.Join();
        }
    }
}
