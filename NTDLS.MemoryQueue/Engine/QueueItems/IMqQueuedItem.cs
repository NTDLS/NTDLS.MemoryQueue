namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// All items that can sit in the queue must inherit INmqQueuedItem.
    /// </summary>
    internal interface IMqQueuedItem
    {
        public DateTime CreatedDate { get; }
        public string PayloadJson { get; }
        public double AgeInSeconds { get; }

        public void RecordSuccessfulDistribution(Guid connectionId);
        public void RecordUnsuccessfulDistribution(Guid connectionId);
        public bool IsDistributionComplete(Guid connectionId);
        public bool IsDistributionComplete(IMqQueuedItem item, HashSet<Guid> subscribers);
    }
}
