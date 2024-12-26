using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NTDLS.MemoryQueue;
using NTDLS.MemoryQueue.Management;

namespace NTDLS.MemoryQueueServer.Pages
{
    public class MessagesModel(ILogger<IndexModel> logger, MqServer mqServer) : PageModel
    {
        const int PageSize = 20;

        [BindProperty(SupportsGet = true)]
        public string QueueName { get; set; } = string.Empty;
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 0;
        public string? ErrorMessage { get; set; }

        private readonly ILogger<IndexModel> _logger = logger;
        public List<MqEnqueuedMessageInformation> Messages { get; set; } = new();

        public void OnGet()
        {
            try
            {
                Messages = mqServer.GetQueueMessages(QueueName, PageNumber * PageSize, PageSize).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetQueueMessages");
                ErrorMessage = ex.Message;
            }
        }
    }
}
