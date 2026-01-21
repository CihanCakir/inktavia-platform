using Aizen.Core.InfoAccessor.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Aizen.Core.InfoAccessor.Extensions.DeviceInfo
{
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseDeviceInfoMiddleware(
            this IApplicationBuilder app,
            Action<AizenDeviceInfoMiddlewareOptions> setupAction = null)
        {
            var setup = new AizenDeviceInfoMiddlewareOptions();
            setupAction?.Invoke(setup);
            app.UseMiddleware<AizenDeviceInfoMiddleware>();

            return app;
        }
    }
}
