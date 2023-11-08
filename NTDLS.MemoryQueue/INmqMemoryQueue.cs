using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue
{
    internal interface INmqMemoryQueue
    {
        internal void InvokeOnConnected(Guid connectionId);
        internal void InvokeOnDisconnected(Guid connectionId);
        internal void InvokeOnNotificationReceived(Guid connectionId, IFrameNotification payload);
    }
}
