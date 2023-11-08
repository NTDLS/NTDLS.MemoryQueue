using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ClientBound
{
    /// <summary>
    /// This is a message that is sent from the server to a client when the queues are being processed.
    /// </summary>
    internal class NmqClientBoundMessage : IFrameNotification
    {
        public string PayloadJson { get; set; }
        public string PayloadType { get; set; }

        public NmqClientBoundMessage(string payloadJson, string payloadType)
        {
            PayloadJson = payloadJson;
            PayloadType = payloadType;
        }
    }
}
