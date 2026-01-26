using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aizen.Core.Realtime.Abstraction.Interfaces;

namespace Aizen.Core.Realtime.Integration
{
    public class CqrsEventSubscriber : BackgroundService
    {
        private readonly ILogger<CqrsEventSubscriber> _logger;
        private readonly IRealtimeEventIngress _ingress;

        public CqrsEventSubscriber(ILogger<CqrsEventSubscriber> logger, IRealtimeEventIngress ingress)
        {
            _logger = logger;
            _ingress = ingress;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CqrsEventSubscriber started (placeholder). Implement ICqrsEventBus subscription if available.");
            return Task.CompletedTask;
        }
    }
}