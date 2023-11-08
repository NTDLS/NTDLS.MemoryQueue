using NTDLS.MemoryQueue.Engine.Payloads.ClientBound;
using NTDLS.MemoryQueue.Engine.QueueItems;
using NTDLS.Semaphore;

namespace NTDLS.MemoryQueue.Engine
{
    /// <summary>
    /// A message queue. The queue manages its own subscribers, messages and is responsible for distribution of those messages to the respective subscribers.
    /// </summary>
    internal class MqQueue
    {
        private readonly Thread _distributionThread;
        private bool _keepRunning = false;
        private readonly MqQueueCollectionManager _queueCollectionManager;

        /// <summary>
        /// The ToLowered name of the queue.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// The configuration that was used to create the queue.
        /// </summary>
        public MqQueueConfiguration Configuration { get; private set; }

        /// <summary>
        /// The ConnectionId of the connection that requested a subscription to this queue.
        /// </summary>
        public HashSet<Guid> Subscribers { get; private set; } = new();

        /// <summary>
        /// The messages that are waiting in the queue.
        /// </summary>
        public CriticalResource<List<IMqQueuedItem>> Messages { get; private set; } = new();

        /// <summary>
        /// Creates and starts a new queue.
        /// </summary>
        /// <param name="queueManager">A reference to the queue manager.</param>
        /// <param name="configuration">The configuration that is used to define the parameters of the new queue.</param>
        public MqQueue(MqQueueCollectionManager queueManager, MqQueueConfiguration configuration)
        {
            Configuration = configuration;
            Key = configuration.Name.ToLower();

            _queueCollectionManager = queueManager;
            _keepRunning = true;
            _distributionThread = new Thread(DistributionThreadProc);
            _distributionThread.Start();
        }

        /// <summary>
        /// Stops the distribution thread and shutsdown the queue.
        /// </summary>
        /// <param name="waitForThreadToExit">Whether or not to wait on the distribution thread to exit after shutdown.</param>
        public void Shutdown(bool waitForThreadToExit)
        {
            _keepRunning = false;
            if (waitForThreadToExit)
            {
                _distributionThread.Join();
            }
        }

        /// <summary>
        /// Adds a single message to the queue.
        /// </summary>
        /// <param name="payloadJson">The json of the object which is the subject of the message.</param>
        /// <param name="payloadType">The original type of the payload.</param>
        public void AddMessage(string payloadJson, string payloadType)
            => Messages.Use((o) => o.Add(new MqQueuedMessage(payloadJson, payloadType)));

        /// <summary>
        /// Adds a single query to the queue.
        /// </summary>
        /// <param name="originationId">The id of the connection that called the method.</param>
        /// <param name="queryId">The unique id of the query. Used to tie the query to the reply.</param>
        /// <param name="payloadJson">The json of the object which is the subject of the query.</param>
        /// <param name="payloadType">The original type of the payload.</param>
        /// <param name="replyType">The type of the reply object. The json should be deserilizable to this type.</param>
        public void AddQuery(Guid originationId, Guid queryId, string payloadJson, string payloadType, string replyType)
            => Messages.Use((o) => o.Add(new MqQueuedQuery(originationId, queryId, payloadJson, payloadType, replyType)));

        /// <summary>
        /// Adds a single query-reply to the queue.
        /// </summary>
        /// <param name="originationId">The id of the connection that called the method.</param>
        /// <param name="queryId">The unique id of the query. Used to tie the query to the reply.</param>
        /// <param name="payloadJson">The json of the object which is the subject of the query-reply.</param>
        /// <param name="payloadType">The original type of the payload.</param>
        /// <param name="replyType">The type of the reply object. The json should be deserilizable to this type.</param>
        public void AddQueryReply(Guid originationId, Guid queryId, string payloadJson, string payloadType, string replyType)
            => Messages.Use((o) => o.Add(new MqQueuedQueryReply(originationId, queryId, payloadJson, payloadType, replyType)));

        /// <summary>
        /// Subscribes a given connection to the queue.
        /// </summary>
        /// <param name="connectionId">The connection id that wants to subscribe.</param>
        public void Subscribe(Guid connectionId)
            => Subscribers.Add(connectionId);

        /// <summary>
        /// Unsubscribes a given connection from the queue.
        /// </summary>
        /// <param name="connectionId">The connection id that wants to unsubscribe.</param>
        public void Unsubscribe(Guid connectionId)
            => Subscribers.Remove(connectionId);

        /// <summary>
        /// The thread that is responsible for distributing messages to the subscribers.
        /// </summary>
        private void DistributionThreadProc()
        {
            Thread.CurrentThread.Name = $"NmqQueue:DistributionThreadProc:{Environment.CurrentManagedThreadId}";

            Utility.EnsureNotNull(_queueCollectionManager.Server);

            while (_keepRunning)
            {
                HashSet<Guid>? subscribers = null;

                var message = Messages.Use((o) =>
                 {
                     subscribers = new HashSet<Guid>(Subscribers); //ClLone the subscribers.
                     if (o.Any())
                     {
                         return o[0];
                     }
                     return null;
                 });

                if (message == null || subscribers == null)
                {
                    Thread.Sleep(5); //If there are no messages, give the CPU a break.
                    continue;
                }

                //Distribute the message.
                foreach (var subscriber in subscribers)
                {
                    try
                    {
                        if (message.SatisfiedSubscribers.Contains(subscriber) == false) //Make sure we have not already sent this message to this subscriber. 
                        {
                            if (message is MqQueuedMessage queuedMessage)
                            {
                                _queueCollectionManager.Server.Notify(subscriber, new MqClientBoundMessage(queuedMessage.PayloadJson, queuedMessage.PayloadType));
                            }
                            else if (message is MqQueuedQuery queuedQuery)
                            {
                                _queueCollectionManager.Server.Notify(subscriber, new MqClientBoundQuery(Configuration.Name,
                                    queuedQuery.QueryId, queuedQuery.PayloadJson, queuedQuery.PayloadType, queuedQuery.ReplyType));
                            }
                            else if (message is MqQueuedQueryReply queuedQueryReply)
                            {
                                if (subscriber == queuedQueryReply.OriginationId) //Only send the reply to the connection that originated the query.
                                {
                                    _queueCollectionManager.Server.Notify(subscriber,
                                        new MqClientBoundQueryReply(Configuration.Name, queuedQueryReply.OriginationId,
                                            queuedQueryReply.QueryId, queuedQueryReply.PayloadJson, queuedQueryReply.PayloadType, queuedQueryReply.ReplyType));
                                }
                            }
                            message.SatisfiedSubscribers.Add(subscriber);
                        }
                    }
                    catch
                    {
                        //TODO: keep a count of attempts to send this message to this subscriber so we can give up after a given number of attempts.
                    }
                }

                if (subscribers.Except(message.SatisfiedSubscribers).Any() == false)
                {
                    //When distribution is successful to all subscribers, remove the message from the queue.
                    Messages.Use((o) => o.RemoveAt(0));
                }
            }
        }
    }
}
