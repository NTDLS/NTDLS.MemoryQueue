namespace NTDLS.MemoryQueue.Engine
{
    internal class NmqQueuedQueryReply : INmqQueuedItem
    {
        /// <summary>
        /// The ID of the client connection that expects a response to this query.
        /// </summary>
        public Guid OriginationId { get; set; }
        /// <summary>
        /// The id of the query that this reply is in response to.
        /// </summary>
        public Guid QueryId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string Payload { get; private set; } = string.Empty;

        /// <summary>
        /// A list of the subscribers that the message has been sent to or that have had too many retries.
        /// </summary>
        public HashSet<Guid> SatisfiedSubscribers { get; set; } = new();

        public NmqQueuedQueryReply(Guid originationId, Guid queryId, string payload)
        {
            OriginationId = originationId;
            Payload = payload;
            QueryId = queryId;
        }

        public NmqQueuedQueryReply()
        {
        }
    }
}
