using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ServerBound
{
    /// <summary>
    /// This is a queue subscription request that is sent from the client to the server.
    /// </summary>
    internal class NmqSubscribe : IFrameNotification
    {
        public string QueueName { get; set; }

        public NmqSubscribe(string queueName)
        {
            QueueName = queueName;
        }
    }
}
