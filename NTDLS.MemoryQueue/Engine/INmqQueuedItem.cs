namespace NTDLS.MemoryQueue.Engine
{
    internal interface INmqQueuedItem
    {
        public DateTime CreatedDate { get; }
        public string PayloadJson { get; }
        public HashSet<Guid> SatisfiedSubscribers { get; }
    }
}
