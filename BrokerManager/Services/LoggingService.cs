using BrokerManager.Interfaces;
using BrokerManager.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrokerManager.Services
{
    public class LoggingService : ILogging
    {
        private readonly ILogger<LoggingService> _logger;
        private readonly LogSettings _config;

        // Constructor to set up logging features
        public LoggingService(ILogger<LoggingService> logger, IOptions<LogSettings> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        // The call that does the actual logging
        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogError(string message)
        {
            _logger.LogError(message);
        }
    }
}
