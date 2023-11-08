using NTDLS.Semaphore;

namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// All items that can sit in the queue must inherit INmqQueuedItem.
    /// </summary>
    internal class MqQueuedItemBase : IMqQueuedItem
    {
        public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
        public string PayloadJson { get; private set; }
        public string PayloadType { get; private set; }

        public MqQueuedItemBase(string payloadJson, string payloadType)
        {
            PayloadJson = payloadJson;
            PayloadType = payloadType;
        }

        /// <summary>
        /// A list of the subscribers that the message has been sent to or that have had too many retries.
        /// </summary>
        private readonly CriticalResource<List<MqDistributionMetrics>> _distributionMetrics = new();

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

        //int maxRetries = 10;

        public bool IsDistributionComplete(HashSet<Guid> subscribers)
        {
            return _distributionMetrics.Use((o) =>
            {
                var completeDistributions =
                    o.Where(o => o.Success == true || o.DistributionAttempts >= maxRetries).Select(o => o.ConnectionId).ToHashSet();

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
