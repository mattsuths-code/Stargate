namespace StarGate.Server.Infrastructure.Logging.Interface
{
    /// <summary>
    /// Provides a lightweight logging abstraction for application components.
    /// Implementations should surface logs to the configured logging sink.
    /// </summary>
    public interface IStarGateLogger
    {
        /// <summary>
        /// Logs an error-level message.
        /// </summary>
        /// <param name="message">The message to log. Indicates a failure or exception that requires attention.</param>
        void LogError(Exception ex);

        /// <summary>
        /// Logs an information-level message.
        /// </summary>
        /// <param name="message">The message to log. Should be a concise, human-readable description of the event.</param>
        void LogInformation(string message);
    }
}