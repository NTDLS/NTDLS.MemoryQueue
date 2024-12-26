using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NTDLS.MemoryQueue;
using NTDLS.MemoryQueue.Management;

namespace NTDLS.MemoryQueueServer.Pages
{
    public class QueueModel(ILogger<IndexModel> logger, MqServer mqServer) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string QueueName { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }

        private readonly ILogger<IndexModel> _logger = logger;
        public MqQueueInformation Queue { get; private set; } = new();
        public List<MqSubscriberInformation> Subscribers { get; set; } = new();

        public void OnGet()
        {
            try
            {
                Queue = mqServer.GetQueues().Where(o => o.QueueName.Equals(QueueName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault() ?? new();
                Subscribers = mqServer.GetSubscribers(Queue.QueueName).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetQueues/GetSubscribers");
                ErrorMessage = ex.Message;
            }
        }
    }
}
