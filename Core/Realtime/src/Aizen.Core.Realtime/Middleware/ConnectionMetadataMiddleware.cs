using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Aizen.Core.Realtime.Abstraction.Models;

namespace Aizen.Core.Realtime.Middleware
{
    public class ConnectionMetadataMiddleware
    {
        private readonly RequestDelegate _next;
        public ConnectionMetadataMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var tenantId = context.Request.Headers["X-Tenant-Id"].ToString();
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            var metadata = new ConnectionMetadata
            {
                RemoteIp = remoteIp,
                UserAgent = userAgent,
                Items = new System.Collections.Generic.Dictionary<string, string>()
            };
            if (!string.IsNullOrEmpty(tenantId)) metadata.Items!["tenantId"] = tenantId;

            context.Items["Aizen.ConnectionMetadata"] = metadata;
            await _next(context);
        }
    }

    public static class ConnectionMetadataMiddlewareExtensions
    {
        public static IApplicationBuilder UseConnectionMetadata(this IApplicationBuilder app)
            => app.UseMiddleware<ConnectionMetadataMiddleware>();
    }
}