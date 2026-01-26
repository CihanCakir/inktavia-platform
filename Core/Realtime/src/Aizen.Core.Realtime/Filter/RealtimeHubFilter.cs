using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aizen.Core.Realtime.Abstraction.Interfaces;

namespace Aizen.Core.Realtime.Filters
{
    public class RealtimeHubFilter : IHubFilter
    {
        private readonly IRateLimitGuard _rateLimitGuard;
        private readonly ILogger<RealtimeHubFilter> _logger;

        public RealtimeHubFilter(IRateLimitGuard rateLimitGuard, ILogger<RealtimeHubFilter> logger)
        {
            _rateLimitGuard = rateLimitGuard;
            _logger = logger;
        }

        public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
        {
            if (invocationContext.HubMethodArguments.Count > 0 && invocationContext.HubMethodArguments[0] is string text)
            {
                if (!_rateLimitGuard.IsMessageLengthAllowed(text))
                    throw new HubException("Message too long");
                var allowed = await _rateLimitGuard.CheckRateLimitAsync(invocationContext.Context.ConnectionId);
                if (!allowed) throw new HubException("Rate limit exceeded");
            }

            return await next(invocationContext);
        }

        public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
        {
            _logger.LogDebug("Connected: {connectionId}", context.Context.ConnectionId);
            return next(context);
        }

        public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
        {
            _logger.LogDebug("Disconnected: {connectionId}", context.Context.ConnectionId);
            return next(context, exception);
        }
    }
}