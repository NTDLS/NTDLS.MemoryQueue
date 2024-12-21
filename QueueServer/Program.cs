using NTDLS.MemoryQueue;

namespace QueueServer
{
    internal class Program
    {
        static void Main()
        {
            var server = new MqServer();
            server.Start(45784);
        }
    }
}
