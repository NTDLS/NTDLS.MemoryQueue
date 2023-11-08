using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.InternalCommunication
{
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
