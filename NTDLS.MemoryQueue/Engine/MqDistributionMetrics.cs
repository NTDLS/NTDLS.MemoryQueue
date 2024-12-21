namespace NTDLS.MemoryQueue.Engine
{
    internal class MqDistributionMetrics(Guid connectionId)
    {
        public Guid ConnectionId { get; set; } = connectionId;
        public bool Success { get; set; }
        public uint DistributionAttempts { get; set; }
    }
}
