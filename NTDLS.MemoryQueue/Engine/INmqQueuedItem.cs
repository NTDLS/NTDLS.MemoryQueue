namespace NTDLS.MemoryQueue.Engine
{
    internal interface INmqQueuedItem
    {
        public DateTime CreatedDate { get; }
        public string Payload { get; }
        public HashSet<Guid> SatisfiedSubscribers { get; }
    }
}
