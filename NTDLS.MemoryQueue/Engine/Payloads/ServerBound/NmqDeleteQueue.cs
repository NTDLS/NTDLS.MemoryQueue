using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// <summary>
    /// This is a delete queue request that is sent from the client to the server.
    /// </summary>
    internal class NmqDeleteQueue : IFrameNotification
    {
        public string QueueName { get; set; }

        public NmqDeleteQueue(string queueName)
        {
            QueueName = queueName;
        }
    }
}
