using NTDLS.MemoryQueue;

namespace ChatLibrary
{
    public class ChatMessage(Guid clientId, string? text) : IMqMessage
    {
        public Guid ClientId { get; set; } = clientId;
        public string? Text { get; set; } = text;
    }
}