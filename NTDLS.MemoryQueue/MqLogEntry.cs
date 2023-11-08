using static NTDLS.MemoryQueue.MqTypes;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Represents a single log entry.
    /// </summary>
    public class MqLogEntry
    {
        /// <summary>
        /// The UTC occurrence date/time of the log entry.
        /// </summary>
        public DateTime OccurrenceDateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The sevirity of the log entry.
        /// </summary>
        public MqLogSeverity Severity { get; set; }

        /// <summary>
        /// The message associated with the log entry.
        /// </summary>
        public string Message { get; set; } = string.Empty;


        /// <summary>
        /// Creates a empty log entry.
        /// </summary>
        public MqLogEntry()
        {
        }

        /// <summary>
        /// Creates an exception log entry from an exception.
        /// </summary>
        /// <param name="ex"></param>
        public MqLogEntry(Exception ex)
        {
            Severity = MqLogSeverity.Exception;
            Message = ex.Message;
        }

        /// <summary>
        /// Creates an exception log entry from an exception and an custom message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public MqLogEntry(string message, Exception ex)
        {
            Severity = MqLogSeverity.Exception;
            Message = $"{message} : {ex.Message}";
        }

        /// <summary>
        /// Creates a custom log entry.
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        public MqLogEntry(MqLogSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

    }
}
