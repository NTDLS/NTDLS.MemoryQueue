using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// The interface which is implemented by both MqServer and MqClient.
    /// </summary>
    public interface IMqMemoryQueue
    {
        internal void InvokeOnConnected(Guid connectionId);
        internal void InvokeOnDisconnected(Guid connectionId);
        internal void InvokeOnNotificationReceived(Guid connectionId, IFramePayloadNotification payload);
        internal IFramePayloadQueryReply InvokeOnQueryReceived(Guid connectionId, IFramePayloadQuery payload);
        internal void WriteLog(IMqMemoryQueue sender, MqLogEntry entry);
    }
}
