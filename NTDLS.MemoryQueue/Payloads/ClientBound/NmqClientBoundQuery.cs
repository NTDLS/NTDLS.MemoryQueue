using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ClientBound
{
    /// <summary>
    /// This is a query that is sent from the server to a client when the queues are being processed.
    /// </summary>
    internal class NmqClientBoundQuery : IFrameNotification
    {
        public string QueueName { get; set; } = string.Empty;
        public Guid QueryId { get; set; }
        public string? Payload { get; set; }

        public NmqClientBoundQuery()
        {
        }

        public NmqClientBoundQuery(string queueName, Guid queryId, string payload)
        {
            QueueName = queueName;
            QueryId = queryId;
            Payload = payload;
        }
    }
}
