using NTDLS.MemoryQueue;

namespace ChatLibrary
{
    public class ChatMessage : IMqMessage
    {
        public Guid ClientId { get; set; }
        public string? Text { get; set; }

        public ChatMessage(Guid clientId, string? text)
        {
            ClientId = clientId;
            Text = text;
        }
    }
}