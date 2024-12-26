﻿using NTDLS.Helpers;
using NTDLS.MemoryQueue.Management;
using NTDLS.Semaphore;
using static NTDLS.MemoryQueue.MqClient;

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
        public PessimisticCriticalResource<Dictionary<Guid, MqSubscriberInformation>> Subscribers { get; set; } = new();

        /// <summary>
        /// Messages that are enqueued in this list.
        /// </summary>
        public PessimisticCriticalResource<List<EnqueuedMessage>> EnqueuedMessages { get; set; } = new();
        public MqQueueConfiguration QueueConfiguration { get; private set; } = queueConfiguration;
        public Thread DeliveryThread = new(DeliveryThreadProc);

        public ulong TotalEnqueuedMessages { get; set; }
        public ulong TotalExpiredMessages { get; set; }
        public ulong TotalDeliveredMessages { get; set; }
        public ulong TotalDeliveryFailures { get; set; }

        private static void DeliveryThreadProc(object? pMessageQueue)
        {
            var messageQueue = pMessageQueue.EnsureNotNull<MessageQueue>();

            var lastStaleMessageScan = DateTime.UtcNow;
            var lastBatchDelivery = DateTime.UtcNow;

            while (messageQueue.KeepRunning)
            {
                int successfulDeliveries = 0; //Just used to omit waiting. We want to spin fast when we are delivering messages.

                try
                {
                    if (messageQueue.QueueConfiguration.BatchDeliveryInterval > TimeSpan.Zero)
                    {
                        if (DateTime.UtcNow - lastBatchDelivery < messageQueue.QueueConfiguration.BatchDeliveryInterval)
                        {
                            Thread.Sleep(1);
                            continue;
                        }
                    }

                    lastBatchDelivery = DateTime.UtcNow;

                    EnqueuedMessage? topMessage = null;
                    List<MqSubscriberInformation>? yetToBeDeliveredSubscribers = null;

                    #region Get message and its subscribers.

                    messageQueue.EnqueuedMessages.TryUseAll([messageQueue.Subscribers], m =>
                    {
                        messageQueue.Subscribers.Use(s => //This lock is already held.
                        {
                            if (messageQueue.QueueConfiguration.MaxMessageAge > TimeSpan.Zero
                            && (DateTime.UtcNow - lastStaleMessageScan).TotalSeconds >= 10)
                            {
                                //If MaxMessageAge is defined, then remove stale messages.
                                messageQueue.TotalExpiredMessages
                                    += (ulong)m.RemoveAll(o => (DateTime.UtcNow - o.Timestamp) > messageQueue.QueueConfiguration.MaxMessageAge);

                                //There could be a lot of messages in the queue, so lets use lastStaleMessageScan
                                //  to not needlessly compare the timestamps each-and-every loop.
                                lastStaleMessageScan = DateTime.UtcNow;
                            }

                            //We only process a queue if it has subscribers.
                            //This is so we do not discard messages as delivered for queues with no subscribers.
                            if (s.Count > 0)
                            {
                                topMessage = m.FirstOrDefault(); //Get the first message in the list, if any.

                                if (topMessage != null)
                                {
                                    //Get list of subscribers that have yet to get a copy of the message.

                                    var yetToBeDeliveredSubscriberIds = s.Keys.Except(topMessage.SatisfiedSubscribersConnectionIDs).ToList();

                                    yetToBeDeliveredSubscribers = s.Where(o => yetToBeDeliveredSubscriberIds.Contains(o.Key)).Select(o => o.Value).ToList();

                                    if (messageQueue.QueueConfiguration.DeliveryScheme == DeliveryScheme.Random)
                                    {
                                        yetToBeDeliveredSubscribers = yetToBeDeliveredSubscribers.OrderBy(_ => Guid.NewGuid()).ToList();
                                    }
                                }
                            }
                        });
                    });

                    #endregion

                    //We we have a message, deliver it to the queue subscribers.
                    if (topMessage != null && yetToBeDeliveredSubscribers != null)
                    {
                        bool successfulDeliveryAndConsume = false;

                        #region Deliver message to subscibers.

                        foreach (var subscriber in yetToBeDeliveredSubscribers)
                        {
                            if (messageQueue.KeepRunning == false)
                            {
                                break;
                            }

                            try
                            {
                                subscriber.DeliveryAttempts++;

                                if (messageQueue.QueueServer.DeliverMessage(subscriber.ConnectionId, messageQueue.QueueConfiguration.QueueName, topMessage))
                                {
                                    subscriber.ConsumedMessages++;
                                    successfulDeliveryAndConsume = true;
                                }

                                messageQueue.TotalDeliveredMessages++;
                                subscriber.SuccessfulMessagesDeliveries++;

                                if (messageQueue.QueueConfiguration.DeliveryThrottle > TimeSpan.Zero)
                                {
                                    if (messageQueue.QueueConfiguration.DeliveryThrottle.TotalSeconds >= 1)
                                    {
                                        double sleepSeconds = messageQueue.QueueConfiguration.DeliveryThrottle.TotalSeconds;
                                        for (int sleep = 0; sleep < sleepSeconds && messageQueue.KeepRunning; sleep++)
                                        {
                                            Thread.Sleep(1000);
                                        }
                                    }
                                    else
                                    {
                                        Thread.Sleep((int)messageQueue.QueueConfiguration.DeliveryThrottle.TotalMilliseconds);
                                    }
                                }

                                if (messageQueue.KeepRunning == false)
                                {
                                    break;
                                }

                                //This thread is the only place we manage [SatisfiedSubscribersConnectionIDs], so we can use it without additional locking.
                                topMessage.SatisfiedSubscribersConnectionIDs.Add(subscriber.ConnectionId);
                                successfulDeliveries++;

                                if (successfulDeliveryAndConsume
                                    && messageQueue.QueueConfiguration.ConsumptionScheme == ConsumptionScheme.FirstConsumedSubscriber)
                                {
                                    //Message was delivered and consumed, break the delivery loop so the message can be removed from the queue.
                                    break;
                                }
                            }
                            catch (Exception ex) //Delivery failure.
                            {
                                messageQueue.TotalDeliveryFailures++;
                                subscriber.FailedMessagesDeliveries++;
                                messageQueue.QueueServer.InvokeOnException(messageQueue.QueueServer, messageQueue.QueueConfiguration, ex.GetBaseException());
                            }

                            //Keep track of per-message-subscriber delivery metrics.
                            if (topMessage.SubscriberMessageDeliveries.TryGetValue(subscriber.ConnectionId, out var subscriberMessageDelivery))
                            {
                                subscriberMessageDelivery.DeliveryAttempts++;
                            }
                            else
                            {
                                subscriberMessageDelivery = new SubscriberMessageDelivery() { DeliveryAttempts = 1 };
                                topMessage.SubscriberMessageDeliveries.Add(subscriber.ConnectionId, subscriberMessageDelivery);
                            }

                            //If we have tried to deliver this message to this subscriber too many times, then mark this subscriber-message as satisfied.
                            if (messageQueue.QueueConfiguration.MaxDeliveryAttempts > 0
                                && subscriberMessageDelivery.DeliveryAttempts >= messageQueue.QueueConfiguration.MaxDeliveryAttempts)
                            {
                                topMessage.SatisfiedSubscribersConnectionIDs.Add(subscriber.ConnectionId);
                            }
                        }

                        #endregion

                        if (messageQueue.KeepRunning == false)
                        {
                            break;
                        }

                        #region Remove message from queue.

                        messageQueue.EnqueuedMessages.UseAll([messageQueue.Subscribers], m =>
                        {
                            messageQueue.Subscribers.Use(s => //This lock is already held.
                            {
                                if (successfulDeliveryAndConsume && messageQueue.QueueConfiguration.ConsumptionScheme == ConsumptionScheme.FirstConsumedSubscriber)
                                {
                                    //The message was consumed by a subscriber, remove it from the message list.
                                    m.Remove(topMessage);
                                }
                                else if (s.Keys.Except(topMessage.SatisfiedSubscribersConnectionIDs).Any() == false)
                                {
                                    //If all subscribers are satisfied (delivered or max attempts reached), then remove the message.
                                    m.Remove(topMessage);
                                }
                            });
                        });

                        #endregion

                        if (messageQueue.KeepRunning == false)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    messageQueue.QueueServer.InvokeOnException(messageQueue.QueueServer, messageQueue.QueueConfiguration, ex.GetBaseException());
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
