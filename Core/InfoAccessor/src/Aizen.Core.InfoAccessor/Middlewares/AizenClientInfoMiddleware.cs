using Aizen.Core.Common.Abstraction.Enums;
using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.InfoAccessor.Middlewares
{
    internal class AizenClientInfoMiddleware
    {
        private readonly RequestDelegate _next;

        public AizenClientInfoMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var seyirInfoAccessor = (IAizenInfoAccessor)httpContext.RequestServices.GetRequiredService(typeof(IAizenInfoAccessor));
            if (seyirInfoAccessor.ClientInfoAccessor.ClientInfo!= null)
            {
                await this._next(httpContext);
                return;
            }

            AizenClientInfo clientInfo;
            if (httpContext?.Request?.Headers == null)
            {
                clientInfo = AizenClientInfo.Unknown;
            }
            else
            {
                _ = Enum.TryParse(httpContext.Request.Headers["X-Aizen-Channel-Type"],
                out ChannelType channelType);

                clientInfo = new AizenClientInfo()
                {
                    RequestId = string.IsNullOrEmpty(httpContext.Request.Headers["X-Aizen-request-Id"])
                        ? httpContext.TraceIdentifier
                        : httpContext.Request.Headers["X-Aizen-Request-Id"].ToString(),
                    AppName = httpContext.Request.Headers["X-Aizen-App-Name"],
                    AppVersion = httpContext.Request.Headers["X-Aizen-App-Version"],
                    ChannelName = httpContext.Request.Headers["X-Aizen-Channel-Name"],
                    Language = httpContext.Request.Headers["Accept-Language"],
                    ChannelType = channelType,
                    TenantCode = httpContext.Request.Headers["X-Aizen-Tenant-Code"],
                    AuthToken = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", ""),
                };
            }

            var container = (IAizenInfoContainer)httpContext.RequestServices.GetRequiredService(typeof(IAizenInfoContainer));
            container.Set(clientInfo);

            await this._next(httpContext);
        }
    }
}
