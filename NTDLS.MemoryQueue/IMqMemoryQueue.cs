using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue
{
    internal interface IMqMemoryQueue
    {
        internal void InvokeOnConnected(Guid connectionId);
        internal void InvokeOnDisconnected(Guid connectionId);
        internal void InvokeOnNotificationReceived(Guid connectionId, IFrameNotification payload);
        internal IFrameQueryReply InvokeOnQueryReceived(Guid connectionId, IFrameQuery payload);
    }
}
