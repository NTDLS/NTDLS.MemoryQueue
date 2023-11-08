using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ServerBound
{
    /// <summary>
    /// /// This is a queue remove subscription request that is sent from the client to the server.
    /// </summary>
    internal class NmqUnsubscribe : IFrameNotification
    {
        public string QueueName { get; set; }

        public NmqUnsubscribe(string queueName)
        {
            QueueName = queueName;
        }
    }
}
