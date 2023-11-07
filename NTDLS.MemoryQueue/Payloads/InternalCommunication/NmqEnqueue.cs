using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.InternalCommunication
{
    internal class NmqEnqueue : IFrameNotification
    {
        public string QueueName { get; set; }
        public string Payload { get; set; }

        public NmqEnqueue(string queueName, string text)
        {
            QueueName = queueName;
            Payload = text;
        }
    }
}
