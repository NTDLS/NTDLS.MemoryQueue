using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.InternalCommunication
{
    internal class NmqDeleteQueue : IFrameNotification
    {
        public string QueueName { get; set; }

        public NmqDeleteQueue(string queueName)
        {
            QueueName = queueName;
        }
    }
}
