using Microsoft.EntityFrameworkCore;
using StarGate.Server.Data;
using StarGate.Server.Infrastructure.Logging.Interface;

namespace StarGate.Server.Infrastructure.Logging.Implementation
{
    /// <summary>
    /// Concrete implementation of <see cref="IStarGateLogger"/> responsible for emitting application logs.
    /// </summary>
    public class StarGateLogger : IStarGateLogger
    {
        private StargateContext _context;
        
        public StarGateLogger(StargateContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Logs an error-level message.
        /// </summary>
        /// <param name="message">The error message to log. Should describe the failure or unexpected condition.</param>
        /// <exception cref="NotImplementedException">Thrown until the logger implementation is provided.</exception>
        public async void LogError(Exception ex)
        {
            var stargateLog = new StargateLog
            {
                LogLevel = "Error",
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                TimeStamp = DateTime.UtcNow
            };

            await _context.StarGateLogs.AddAsync(stargateLog);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Logs an information-level message.
        /// </summary>
        /// <param name="message">The informational message to log. Should describe normal application events.</param>
        /// <exception cref="NotImplementedException">Thrown until the logger implementation is provided.</exception>
        public async void LogInformation(string message)
        {
            var stargateLog = new StargateLog
            {
                LogLevel = "Info",
                Message = message,
                TimeStamp = DateTime.UtcNow
            };

            await _context.StarGateLogs.AddAsync(stargateLog);

            await _context.SaveChangesAsync();
        }
    }
}