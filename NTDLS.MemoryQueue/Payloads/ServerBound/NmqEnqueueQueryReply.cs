using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ServerBound
{
    /// This is a enque query reply request that is sent from the client to the server.
    internal class NmqEnqueueQueryReply : IFrameNotification
    {
        public string QueueName { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public Guid QueryId { get; set; }

        public NmqEnqueueQueryReply(string queueName, Guid queryId, string payload)
        {
            QueueName = queueName;
            Payload = payload;
            QueryId = queryId;
        }

        public NmqEnqueueQueryReply()
        {

        }
    }
}
