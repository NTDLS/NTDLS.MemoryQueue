namespace NTDLS.MemoryQueue.Engine
{
    internal class NmqQueuedQueryReply : INmqQueuedItem
    {
        /// <summary>
        /// The ID of the client connection that expects a response to this query.
        /// </summary>
        public Guid OriginationId { get; private set; }
        /// <summary>
        /// The id of the query that this reply is in response to.
        /// </summary>
        public Guid QueryId { get; private set; }
        public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
        public string Payload { get; private set; }
        public string PayloadType { get; set; }
        public string ReplyType { get; set; }

        /// <summary>
        /// A list of the subscribers that the message has been sent to or that have had too many retries.
        /// </summary>
        public HashSet<Guid> SatisfiedSubscribers { get; set; } = new();

        public NmqQueuedQueryReply(Guid originationId, Guid queryId, string payload, string payloadType, string replyType)
        {
            OriginationId = originationId;
            Payload = payload;
            QueryId = queryId;
            PayloadType = payloadType;
            ReplyType = replyType;
        }
    }
}
