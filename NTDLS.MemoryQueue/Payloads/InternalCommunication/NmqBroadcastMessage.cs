using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.InternalCommunication
{
    internal class NmqBroadcastMessage : IFrameNotification
    {
        public string? Payload { get; set; }

        public NmqBroadcastMessage()
        {
        }

        public NmqBroadcastMessage(string payload)
        {
            Payload = payload;
        }
    }
}
