namespace NTDLS.MemoryQueue.Events
{
    /// <summary>
    /// The parameter for the client OnQueryReceived event.
    /// </summary>
    public class NmqQueryReceivedEventParam
    {
        public object? Payload { get; set; }
    }
}
