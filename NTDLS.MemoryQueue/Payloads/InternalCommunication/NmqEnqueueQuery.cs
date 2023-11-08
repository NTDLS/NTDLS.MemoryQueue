using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.InternalCommunication
{
    internal class NmqEnqueueQuery : IFrameNotification
    {
        public Guid QueryId { get; set; }
        public string QueueName { get; set; }
        public string Payload { get; set; }

        public NmqEnqueueQuery(string queueName, Guid queryId, string payload)
        {
            QueueName = queueName;
            QueryId = queryId;
            Payload = payload;
        }
    }
}
