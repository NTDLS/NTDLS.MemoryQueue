using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ServerBound
{
    /// <summary>
    /// This is a enque message request that is sent from the client to the server.
    /// </summary>
    internal class NmqEnqueueMessage : IFrameNotification
    {
        public string QueueName { get; set; }
        public string Payload { get; set; }

        public NmqEnqueueMessage(string queueName, string payload)
        {
            QueueName = queueName;
            Payload = payload;
        }
    }
}
