using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NTDLS.MemoryQueue;
using NTDLS.MemoryQueue.Management;

namespace NTDLS.MemoryQueueServer.Pages
{
    public class MessageModel(ILogger<IndexModel> logger, MqServer mqServer) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string QueueName { get; set; } = string.Empty;
        [BindProperty(SupportsGet = true)]
        public Guid MessageId { get; set; }

        public string? ErrorMessage { get; set; }

        private readonly ILogger<IndexModel> _logger = logger;
        public MqEnqueuedMessageInformation Message { get; set; } = new();

        public void OnGet()
        {
            try
            {
                Message = mqServer.GetQueueMessage(QueueName, MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetQueueMessage");
                ErrorMessage = ex.Message;
            }
        }
    }
}
