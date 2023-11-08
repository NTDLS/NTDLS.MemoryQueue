using NTDLS.MemoryQueue;

namespace ChatLibrary
{
    public class ChatMessage : IMqMessage
    {
        public string? Text { get; set; }

        public ChatMessage(string? text)
        {
            Text = text;
        }
    }
}