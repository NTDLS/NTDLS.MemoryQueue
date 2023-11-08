namespace NTDLS.MemoryQueue.Engine
{
    internal class QueryWaitingForReply
    {
        public string? Payload { get; private set; }
        public Guid QueryId { get; set; } = Guid.NewGuid();
        public AutoResetEvent Waiter { get; set; } = new AutoResetEvent(false);

        public void SetReplyPayload(string? payload)
        {
            Payload = payload;
        }
    }
}
