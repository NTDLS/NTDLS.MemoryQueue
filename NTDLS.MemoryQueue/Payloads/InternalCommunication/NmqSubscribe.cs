using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.InternalCommunication
{
    internal class NmqSubscribe : IFrameNotification
    {
        public string QueueName { get; set; }

        public NmqSubscribe(string queueName)
        {
            QueueName = queueName;
        }
    }
}
