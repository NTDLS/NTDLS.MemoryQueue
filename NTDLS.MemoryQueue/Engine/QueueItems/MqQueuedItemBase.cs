using NTDLS.Semaphore;

namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// All items that can sit in the queue must inherit INmqQueuedItem.
    /// </summary>
    internal class MqQueuedItemBase(MqQueue queue, string payloadJson, string payloadType)
        : IMqQueuedItem
    {
        public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
        public string PayloadJson { get; private set; } = payloadJson;
        public string PayloadType { get; private set; } = payloadType;
        public double AgeInSeconds => (DateTime.UtcNow - CreatedDate).TotalSeconds;

        /// <summary>
        /// The queue that ownes this item.
        /// </summary>
        public MqQueue Queue { get; private set; } = queue;

        /// <summary>
        /// A list of the subscribers that the message has been sent to or that have had too many retries.
        /// </summary>
        private readonly PessimisticCriticalResource<List<MqDistributionMetrics>> _distributionMetrics = new();

        public void RecordSuccessfulDistribution(Guid connectionId)
        {
            _distributionMetrics.Use((o) =>
            {
                var existingMetrics = o.Where(o => o.ConnectionId == connectionId).SingleOrDefault();
                if (existingMetrics != null)
                {
                    existingMetrics.DistributionAttempts++;
                    existingMetrics.Success = true;
                }
                else
                {
                    o.Add(new MqDistributionMetrics(connectionId)
                    {
                        DistributionAttempts = 1,
                        Success = true
                    });
                }
            });
        }

        public void RecordUnsuccessfulDistribution(Guid connectionId)
        {
            _distributionMetrics.Use((o) =>
            {
                var existingMetrics = o.Where(o => o.ConnectionId == connectionId).SingleOrDefault();
                if (existingMetrics != null)
                {
                    existingMetrics.DistributionAttempts++;
                }
                else
                {
                    o.Add(new MqDistributionMetrics(connectionId)
                    {
                        DistributionAttempts = 1
                    });
                }
            });
        }

        public bool IsDistributionComplete(IMqQueuedItem item, HashSet<Guid> subscribers)
        {
            if (Queue.Configuration.MaxAgeInSeconds > 0 && item.AgeInSeconds >= Queue.Configuration.MaxAgeInSeconds)
            {
                return true; //Expired.
            }

            return _distributionMetrics.Use((o) =>
            {
                //Create a list of all of the successful, expired or exhausted queue distributions.
                var completeDistributions =
                    o.Where(o => o.Success == true //Successful
                        || o.DistributionAttempts >= Queue.Configuration.MaxDistributionAttempts //exhausted
                        ).Select(o => o.ConnectionId).ToHashSet();

                return !subscribers.Except(completeDistributions).Any();
            });
        }

        public bool IsDistributionComplete(Guid connectionId)
        {
            return _distributionMetrics.Use((o)
                => o.Any(o => o.ConnectionId == connectionId));
        }
    }
}
