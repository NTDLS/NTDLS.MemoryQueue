using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads
{
    internal class MqInternalQueryReplyBoolean : IFrameQueryReply
    {
        public delegate void CollapseExceptionToResultProc();

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

        /// <summary>
        /// Tries to execute a function and returns a "true" MqInternalQueryReplyBoolean or an "exception" MqInternalQueryReplyBoolean.
        /// </summary>
        /// <param name="procedureCall"></param>
        /// <returns></returns>
        public static MqInternalQueryReplyBoolean CollapseExceptionToResult(CollapseExceptionToResultProc procedureCall)
        {
            try
            {
                procedureCall();
                return new MqInternalQueryReplyBoolean(true);
            }
            catch (Exception ex)
            {
                return new MqInternalQueryReplyBoolean(ex);
            }
        }

        /// <summary>
        /// Checks every way that I know of to see if a task completed succesfully. If it did not we throw an exception.
        /// </summary>
        /// <param name="task"></param>
        /// <exception cref="Exception"></exception>
        public static void AssertTask(Task<MqInternalQueryReplyBoolean> task)
        {
            if (task.Exception != null)
            {
                throw new Exception($"The task failed. Exception: {task.Exception}");
            }
            else if (task.IsCanceled)
            {
                throw new Exception($"The task was cancelled before completion.");
            }
            else if (task.IsFaulted)
            {
                throw new Exception($"The task faulted with an unknown exception.");
            }
            else if (task.IsCompletedSuccessfully)
            {
                if (task.Result.Value != true)
                {
                    throw new Exception($"The task failed at the remote peer. Exception: {task.Result.Message}");
                }
            }
            else
            {
                throw new Exception($"The task failed with an unknown exception.");
            }
        }
    }
}
