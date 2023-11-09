using NTDLS.StreamFraming;
using NTDLS.StreamFraming.Payloads;
using System.Net.Sockets;

namespace NTDLS.MemoryQueue.Engine
{
    internal class MqPeerConnection
    {
        private readonly FrameBuffer _frameBuffer = new(16 * 1024);
        private readonly TcpClient _tcpClient; //The TCP/IP connection associated with this connection.
        private readonly Thread _dataPumpThread; //The thread that receives data for this connection.
        private readonly NetworkStream _stream; //The stream for the TCP/IP connection (used for reading and writing).
        private readonly IMqMemoryQueue _hub;
        private bool _keepRunning;

        public Guid Id { get; private set; }

        public bool IsHealthy
        {
            get
            {
                try
                {
                    return _tcpClient.Connected && _stream.Socket.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }

        public MqPeerConnection(IMqMemoryQueue hub, TcpClient tcpClient)
        {
            Id = Guid.NewGuid();
            _hub = hub;
            _tcpClient = tcpClient;
            _dataPumpThread = new Thread(DataPumpThreadProc);
            _keepRunning = true;
            _stream = tcpClient.GetStream();
        }

        public void SendNotification(IFrameNotification notification)
            => _stream.WriteNotification(notification);

        public Task<T> SendQuery<T>(IFrameQuery query) where T : IFrameQueryReply
            => _stream.WriteQuery<T>(query);

        public void RunAsync()
        {
            _dataPumpThread.Start();
        }

        internal void DataPumpThreadProc()
        {
            Thread.CurrentThread.Name = $"PeerConnection:DataPumpThreadProc:{Environment.CurrentManagedThreadId}";

            try
            {
                while (_keepRunning && _stream.ReadAndProcessFrames(_frameBuffer,
                    (payload) => _hub.InvokeOnNotificationReceived(Id, payload),
                    (payload) => _hub.InvokeOnQueryReceived(Id, payload))
                    
                    )
                {
                }
            }
            catch (Exception ex)
            {
                //TODO: log this.
            }

            _hub.InvokeOnDisconnected(Id);
        }

        public void Disconnect(bool waitOnThread)
        {
            if (_keepRunning)
            {
                _keepRunning = false;
                try { _stream.Close(); } catch { }
                try { _stream.Dispose(); } catch { }
                try { _tcpClient.Close(); } catch { }
                try { _tcpClient.Dispose(); } catch { }

                if (waitOnThread)
                {
                    _dataPumpThread.Join();
                }
            }
        }
    }
}
