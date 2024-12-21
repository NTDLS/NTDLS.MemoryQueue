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

            var lastStaleMessageScan = DateTime.UtcNow;

            while (messageQueue.KeepRunning)
            {
                int successfulDeliveries = 0; //Just used to omit waiting. We want to spin fast when we are delivering messages.

                try
                {
                    EnqueuedMessage? topMessage = null;
                    List<Guid>? yetToBeDeliveredSubscribers = null;

                    messageQueue.EnqueuedMessages.UseAll([messageQueue.Subscribers], m =>
                    {
                        //We only process a queue if it has subscribers, this is so we do not discard messages for queues with no subscribers.
                        if (messageQueue.QueueConfiguration.MaxMessageAge > TimeSpan.Zero && (DateTime.UtcNow - lastStaleMessageScan).TotalSeconds >= 10)
                        {
                            //If MaxMessageAge is defined, then remove stale messages.
                            m.RemoveAll(o => (DateTime.UtcNow - o.Timestamp) > messageQueue.QueueConfiguration.MaxMessageAge);

                            //There could be a lot of messages in the queue, so lets not needlessly compare the timestamps each-and-every loop.
                            lastStaleMessageScan = DateTime.UtcNow;
                        }

                        messageQueue.Subscribers.Use(s =>
                        {
                            if (s.Count > 0)
                            {
                                topMessage = m.FirstOrDefault(); //Get the first message in the list, if any.

                                if (topMessage != null)
                                {
                                    //Get list of subscribers that have yet to get a copy of the message.
                                    yetToBeDeliveredSubscribers = s.Except(topMessage.SatisfiedSubscribersConnectionIDs).ToList();
                                }
                            }
                        });
                    });

                    //We we have a message, deliver it to the queue subscribers.
                    if (topMessage != null && yetToBeDeliveredSubscribers != null)
                    {
                        foreach (var subscriberId in yetToBeDeliveredSubscribers)
                        {
                            if (messageQueue.QueueServer.DeliverMessage(subscriberId, messageQueue.QueueConfiguration.QueueName, topMessage))
                            {
                                //This thread is the only place we manage [SatisfiedSubscribersConnectionIDs], so we can use it without additional locking.
                                topMessage.SatisfiedSubscribersConnectionIDs.Add(subscriberId);
                                successfulDeliveries++;
                            }

                            //Keep track of per-message-subscriber delivery metrics.
                            if (topMessage.SubscriberMessageDeliveries.TryGetValue(subscriberId, out var subscriberMessageDelivery))
                            {
                                subscriberMessageDelivery.DeliveryAttempts++;
                            }
                            else
                            {
                                subscriberMessageDelivery = new SubscriberMessageDelivery() { DeliveryAttempts = 1 };
                                topMessage.SubscriberMessageDeliveries.Add(subscriberId, subscriberMessageDelivery);
                            }

                            //If we have tried to deliver this message to this subscriber too many times, then mark this subscriber-message as satisfied.
                            if (messageQueue.QueueConfiguration.MaxDeliveryAttempts > 0
                                && subscriberMessageDelivery.DeliveryAttempts >= messageQueue.QueueConfiguration.MaxDeliveryAttempts)
                            {
                                topMessage.SatisfiedSubscribersConnectionIDs.Add(subscriberId);
                            }
                        }

                        messageQueue.EnqueuedMessages.UseAll([messageQueue.Subscribers], m =>
                        {
                            messageQueue.Subscribers.Use(s =>
                            {
                                //If the all message-subscribers are satisfied, then remove the message.
                                if (s.Except(topMessage.SatisfiedSubscribersConnectionIDs).Any() == false)
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
