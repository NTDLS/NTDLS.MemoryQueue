namespace NTDLS.MemoryQueue.Engine
{
    internal class MqDistributionMetrics
    {
        public Guid ConnectionId { get; set; }
        public bool Success { get; set; }
        public uint DistributionAttempts { get; set; }

        public MqDistributionMetrics(Guid connectionId)
        {
            ConnectionId = connectionId;
        }
    }
}
