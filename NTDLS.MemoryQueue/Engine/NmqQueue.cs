using NTDLS.MemoryQueue.Payloads;
using NTDLS.MemoryQueue.Payloads.ClientBound;
using NTDLS.ReliableMessaging;
using NTDLS.Semaphore;

namespace NTDLS.MemoryQueue.Engine
{
    internal class NmqQueue
    {
        private readonly Thread _distributionThread;
        private bool _keepRunning = false;
        private readonly NmqQueueManager _queueManager;

        public string Key { get; private set; }
        public NmqQueueConfiguration Configuration { get; private set; }
        public HashSet<Guid> Subscribers { get; private set; } = new();
        public CriticalResource<List<INmqQueuedItem>> Messages { get; private set; } = new();

        public NmqQueue(NmqQueueManager queueManager, NmqQueueConfiguration config)
        {
            Configuration = config;
            Key = config.Name.ToLower();

            _queueManager = queueManager;
            _keepRunning = true;
            _distributionThread = new Thread(DistributionThreadProc);
            _distributionThread.Start();
        }

        public void Shutdown(bool waitForThreadToExit)
        {
            _keepRunning = false;
            if (waitForThreadToExit)
            {
                _distributionThread.Join();
            }
        }

        public void AddMessage(string payloadJson, string payloadType)
            => Messages.Use((o) => o.Add(new NmqQueuedMessage(payloadJson, payloadType)));

        public void AddQuery(Guid originationId, Guid queryId, string payloadJson, string payloadType, string replyType)
            => Messages.Use((o) => o.Add(new NmqQueuedQuery(originationId, queryId, payloadJson, payloadType, replyType)));

        public void AddQueryReply(Guid originationId, Guid queryId, string payloadJson, string payloadType, string replyType)
            => Messages.Use((o) => o.Add(new NmqQueuedQueryReply(originationId, queryId, payloadJson, payloadType, replyType)));

        public void Subscribe(Guid connectionId)
            => Subscribers.Add(connectionId);

        public void Unsubscribe(Guid connectionId)
            => Subscribers.Remove(connectionId);

        private void DistributionThreadProc()
        {
            Utility.EnsureNotNull(_queueManager.Server);

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
                            if (message is NmqQueuedMessage queuedMessage)
                            {
                                _queueManager.Server.Notify(subscriber, new NmqClientBoundMessage(queuedMessage.PayloadJson, queuedMessage.PayloadType));
                            }
                            else if (message is NmqQueuedQuery queuedQuery)
                            {
                                _queueManager.Server.Notify(subscriber, new NmqClientBoundQuery(Configuration.Name,
                                    queuedQuery.QueryId, queuedQuery.PayloadJson, queuedQuery.PayloadType, queuedQuery.ReplyType));
                            }
                            else if (message is NmqQueuedQueryReply queuedQueryReply)
                            {
                                if (subscriber == queuedQueryReply.OriginationId) //Only send the reply to the connection that originated the query.
                                {
                                    _queueManager.Server.Notify(subscriber,
                                        new NmqClientBoundQueryReply(Configuration.Name, queuedQueryReply.OriginationId,
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
