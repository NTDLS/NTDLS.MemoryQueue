namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// Represents a "physical" query message that is waiting in the queue.
    /// </summary>
    internal class MqQueuedQuery : MqQueuedItemBase, IMqQueuedItem
    {
        /// <summary>
        /// The ID of the client connection that expects a response to this query.
        /// </summary>
        public Guid OriginationId { get; private set; }
        public Guid QueryId { get; private set; }
        public string ReplyType { get; private set; }

        /// <summary>
        /// A list of the subscribers that the message has been sent to or that have had too many retries.
        /// </summary>
        public HashSet<Guid> SatisfiedSubscribers { get; private set; } = new();

        public MqQueuedQuery(Guid originationId, Guid queryId, string payloadJson, string payloadType, string replyType)
            : base(payloadJson, payloadType)
        {
            OriginationId = originationId;
            QueryId = queryId;
            ReplyType = replyType;
        }
    }
}
