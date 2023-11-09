using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads
{
    internal class MqInternalQueryReplyBoolean : IFrameQueryReply
    {
        public bool Value { get; set; }
        public string Message { get; set; } = string.Empty;

        public MqInternalQueryReplyBoolean()
        {
        }

        public MqInternalQueryReplyBoolean(bool value)
        {
            Value = value;
        }

        public MqInternalQueryReplyBoolean(Exception ex)
        {
            Message = ex.Message;   
            Value = false;
        }

        public delegate void TryProc();

        public static MqInternalQueryReplyBoolean Try(TryProc tryProc)
        {
            try
            {
                tryProc();
                return new MqInternalQueryReplyBoolean(true);
            }
            catch (Exception ex)
            {
                return new MqInternalQueryReplyBoolean(ex);
            }
        }
    }
}
