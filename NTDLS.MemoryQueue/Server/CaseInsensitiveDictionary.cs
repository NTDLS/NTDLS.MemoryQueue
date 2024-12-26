namespace NTDLS.MemoryQueue.Server
{
    internal class CaseInsensitiveMessageQueueDictionary : Dictionary<string, MessageQueue>
    {
        public CaseInsensitiveMessageQueueDictionary()
            : base (StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
