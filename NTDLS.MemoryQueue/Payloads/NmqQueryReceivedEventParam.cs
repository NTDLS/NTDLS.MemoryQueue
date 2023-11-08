namespace NTDLS.MemoryQueue.Payloads
{
    /// <summary>
    /// Used to send a query to the client query notification event.
    /// </summary>
    public class NmqQueryReceivedEventParam
    {
        public string? Payload { get; set; }
    }
}
