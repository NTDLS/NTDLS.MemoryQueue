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
        internal void InvokeOnNotificationReceived(Guid connectionId, IFrameNotification payload);
        internal IFrameQueryReply InvokeOnQueryReceived(Guid connectionId, IFrameQuery payload);
        internal void WriteLog(IMqMemoryQueue sender, MqLogEntry entry);
    }
}
