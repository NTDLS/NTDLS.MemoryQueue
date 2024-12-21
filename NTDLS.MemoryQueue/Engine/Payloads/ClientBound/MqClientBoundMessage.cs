using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ClientBound
{
    /// <summary>
    /// This is a message that is sent from the server to a client when the queues are being processed.
    /// </summary>
    internal class MqClientBoundMessage(string payloadJson, string payloadType)
        : IFramePayloadNotification
    {
        public string PayloadJson { get; set; } = payloadJson;
        public string PayloadType { get; set; } = payloadType;
    }
}
