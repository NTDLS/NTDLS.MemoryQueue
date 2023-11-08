namespace NTDLS.MemoryQueue.Engine
{
    internal class NmqQueuedQuery : INmqQueuedItem
    {
        /// <summary>
        /// The ID of the client connection that expects a response to this query.
        /// </summary>
        public Guid OriginationId { get; private set; }
        public Guid QueryId { get; private set; }
        public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
        public string Payload { get; private set; }
        public string PayloadType { get; private set; }
        public string ReplyType { get; private set; }

        /// <summary>
        /// A list of the subscribers that the message has been sent to or that have had too many retries.
        /// </summary>
        public HashSet<Guid> SatisfiedSubscribers { get; private set; } = new();

        public NmqQueuedQuery(Guid originationId, Guid queryId, string payload, string payloadType, string replyType)
        {
            OriginationId = originationId;
            QueryId = queryId;
            Payload = payload;
            PayloadType = payloadType;
            ReplyType = replyType;
        }
    }
}
