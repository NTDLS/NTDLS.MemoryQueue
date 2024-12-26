using Microsoft.AspNetCore.Mvc.RazorPages;
using NTDLS.MemoryQueue;
using NTDLS.MemoryQueue.Management;

namespace NTDLS.MemoryQueueServer.Pages
{
    public class IndexModel(ILogger<IndexModel> logger, MqServer mqServer) : PageModel
    {
        private readonly ILogger<IndexModel> _logger = logger;
        public List<MqQueueInformation> Queues { get; private set; } = new();
        public MqServerInformation ServerConfig = new();
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            try
            {
                ServerConfig = mqServer.GetConfiguration();

                var queues = mqServer.GetQueues();

                foreach (var queue in queues)
                {
                    Queues.Add(queue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetQueues");
                ErrorMessage = ex.Message;
            }
        }
    }
}
