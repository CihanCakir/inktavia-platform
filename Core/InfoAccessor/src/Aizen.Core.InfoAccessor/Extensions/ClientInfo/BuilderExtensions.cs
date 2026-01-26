using Aizen.Core.InfoAccessor.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Aizen.Core.InfoAccessor.Extensions.ClientInfo
{
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseClientInfoMiddleware(
            this IApplicationBuilder app,
            Action<AizenClientInfoMiddlewareOptions> setupAction = null)
        {
            var setup = new AizenClientInfoMiddlewareOptions();
            setupAction?.Invoke(setup);
            app.UseMiddleware<AizenClientInfoMiddleware>();

            return app;
        }
    }
}
