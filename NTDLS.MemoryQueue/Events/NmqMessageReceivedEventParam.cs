namespace NTDLS.MemoryQueue.Events
{
    /// <summary>
    /// The parameter for the client OnMessageReceived event.
    /// </summary>
    public class NmqMessageReceivedEventParam
    {
        public string? Payload { get; set; }
    }
}
