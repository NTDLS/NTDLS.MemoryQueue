using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.InternalCommunication
{
    internal class NmqBroadcastQuery : IFrameNotification
    {
        public string QueueName { get; set; } = string.Empty;
        public Guid QueryId { get; set; }
        public string? Payload { get; set; }

        public NmqBroadcastQuery()
        {
        }

        public NmqBroadcastQuery(string queueName, Guid queryId, string payload)
        {
            QueueName = queueName;
            QueryId = queryId;
            Payload = payload;
        }
    }
}
