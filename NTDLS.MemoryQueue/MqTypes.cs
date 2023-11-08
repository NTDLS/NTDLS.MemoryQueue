namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Public types used by the queue server and client.
    /// </summary>
    public class MqTypes
    {
        /// <summary>
        /// These are the various severities When writing to the log.
        /// </summary>
        public enum MqLogSeverity
        {
            /// <summary>
            /// Extraneous and superfluous information.
            /// </summary>
            Verbose,
            /// <summary>
            /// Basic informational logging.
            /// </summary>
            Information,
            /// <summary>
            /// Warning that are not quite exceptions.
            /// </summary>
            Warning,
            /// <summary>
            /// An error has occured.
            /// </summary>
            Exception
        }
    }
}
