namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// All items that can sit in the queue must inherit INmqQueuedItem.
    /// </summary>
    internal interface IMqQueuedItem
    {
        public DateTime CreatedDate { get; }
        public string PayloadJson { get; }
        public HashSet<Guid> SatisfiedSubscribers { get; }
    }
}
