using Microsoft.AspNetCore.Builder;
using Aizen.Core.InfoAccessor.Middlewares;

namespace Aizen.Core.InfoAccessor.Extensions.UserInfo
{
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseUserInfoMiddleware(
            this IApplicationBuilder app,
            Action<AizenUserInfoMiddlewareOptions> setupAction = null)
        {
            var setup = new AizenUserInfoMiddlewareOptions();
            setupAction?.Invoke(setup);
            app.UseMiddleware<AizenUserInfoMiddleware>();

            return app;
        }
    }
}
