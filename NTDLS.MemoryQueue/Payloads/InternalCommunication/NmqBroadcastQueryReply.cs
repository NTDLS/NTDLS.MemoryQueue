using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.InternalCommunication
{
    internal class NmqBroadcastQueryReply : IFrameNotification
    {
        public string QueueName { get; set; } = string.Empty;
        public Guid QueryId { get; set; }
        public Guid OriginationId { get; set; }
        public string? Payload { get; set; }

        public NmqBroadcastQueryReply()
        {
        }

        public NmqBroadcastQueryReply(string queueName, Guid originationId, Guid queryId, string payload)
        {
            QueueName = queueName;
            QueryId = queryId;
            Payload = payload;
            OriginationId = originationId;
        }
    }
}
