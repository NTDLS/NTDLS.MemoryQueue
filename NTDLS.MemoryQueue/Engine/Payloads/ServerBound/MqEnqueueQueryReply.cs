using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// This is a enqueue query reply request that is sent from the client to the server.
    internal class MqEnqueueQueryReply : IFramePayloadQuery
    {
        public string QueueName { get; set; }
        public string PayloadJson { get; set; }
        public Guid QueryId { get; set; }
        public string PayloadType { get; set; }
        public string ReplyType { get; set; }

        public MqEnqueueQueryReply(string queueName, Guid queryId, string payloadJson, string payloadType, string replyType)
        {
            QueueName = queueName;
            PayloadJson = payloadJson;
            QueryId = queryId;
            PayloadType = payloadType;
            ReplyType = replyType;
        }
    }
}
