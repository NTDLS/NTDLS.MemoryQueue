namespace NTDLS.MemoryQueue.Payloads
{
    /// <summary>
    /// Used to send a message to the client notification event.
    /// </summary>
    public class NmqMessageReceivedEventParam
    {
        public string? Payload { get; set; }
    }
}
