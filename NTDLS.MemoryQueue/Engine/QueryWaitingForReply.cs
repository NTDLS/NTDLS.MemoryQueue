namespace NTDLS.MemoryQueue.Engine
{
    /// <summary>
    /// Ties a QueryId to a wait event so that it can be signaled when (and if) the reply is received.
    /// </summary>
    internal class QueryWaitingForReply
    {
        public string? PayloadJson { get; private set; }
        public Guid QueryId { get; set; } = Guid.NewGuid();
        public AutoResetEvent Waiter { get; set; } = new AutoResetEvent(false);

        public void SetReplyPayload(string? payload)
        {
            PayloadJson = payload;
        }
    }
}
