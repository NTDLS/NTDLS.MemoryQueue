using NTDLS.MemoryQueue;

namespace ChatServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = new MqServer();

            server.Start(45784);

            Console.WriteLine("Press [enter] to shutdown.");
            Console.ReadLine();

            server.Shutdown();
        }
    }
}