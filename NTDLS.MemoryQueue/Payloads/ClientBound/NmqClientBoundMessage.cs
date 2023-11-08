using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ClientBound
{
    /// <summary>
    /// This is a message that is sent from the server to a client when the queues are being processed.
    /// </summary>
    internal class NmqClientBoundMessage : IFrameNotification
    {
        public string Payload { get; set; }

        public NmqClientBoundMessage(string payload)
        {
            Payload = payload;
        }
    }
}
