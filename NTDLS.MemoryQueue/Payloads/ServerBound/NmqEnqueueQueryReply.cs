using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ServerBound
{
    /// This is a enque query reply request that is sent from the client to the server.
    internal class NmqEnqueueQueryReply : IFrameNotification
    {
        public string QueueName { get; set; }
        public string Payload { get; set; }
        public Guid QueryId { get; set; }
        public string PayloadType { get; set; }
        public string ReplyType { get; set; }

        public NmqEnqueueQueryReply(string queueName, Guid queryId, string payload, string payloadType, string replyType)
        {
            QueueName = queueName;
            Payload = payload;
            QueryId = queryId;
            PayloadType = payloadType;
            ReplyType = replyType;
        }
    }
}
