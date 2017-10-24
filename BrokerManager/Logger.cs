using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BrokerManager.Interfaces;
using BrokerManager.Models;

namespace BrokerManager
{
    public class Logger
    {
        private readonly ILogging _logService;
        private readonly ILogger<Logger> _logger;
        private readonly LogSettings _config;

        public Logger(ILogging logService, IOptions<LogSettings> config, ILogger<Logger> logger)
        {
            _logService = logService;
            _logger = logger;
            _config = config.Value;
        }

        public void warningMessage(string message)
        {
            _logService.LogWarning(message);
        }

        public void errorMessage(string message)
        {
            _logService.LogError(message);
        }
    }
}
