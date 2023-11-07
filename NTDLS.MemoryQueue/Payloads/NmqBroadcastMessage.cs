using NTDLS.MemoryQueue.Engine;
using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads
{
    internal class NmqBroadcastMessage : IFrameNotification
    {
        public string? Payload { get; set; }

        public NmqBroadcastMessage()
        {
        }

        public NmqBroadcastMessage(NmqQueuedMessage message)
        {
            Payload = message.Payload;
        }
    }
}
