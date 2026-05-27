using Aizen.Core.Api.Middleware;
using Aizen.Core.Starter.Abstraction;
using Aizen.Core.Starter.Abstraction.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Aizen.Core.InfoAccessor.Extensions.ClientInfo;
using Aizen.Core.InfoAccessor.Extensions.DeviceInfo;
using Aizen.Core.Scheduler.Extensions;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions.UserInfo;

namespace Aizen.Core.Starter.Operation;

public class AizenOperationApplicationConfiguration : IAizenApplicationConfiguration
{
    public AizenAppInfo AppInfo { get; }

    public AizenOperationApplicationConfiguration(AizenAppInfo appInfo)
    {
        AppInfo = appInfo;
    }
    public void Configure(AizenApplication app, IWebHostEnvironment env)
    {
        app.UseMiddleware<AizenRequestResponseMiddleware>();
        app.UseMiddleware<AizenInfoMiddleware>();
        // Api spesifik Middleware kullanımı
        if (this.AppInfo.TypeInclude.Contains(AppType.Api))
        {
            app.UseAizenGlobalExceptionMiddleware();
        }

        // Scheduler spesifik Middleware ve işlevler
        if (this.AppInfo.TypeInclude.Contains(AppType.Scheduler))
        {

            app.UseAizenRecurringJob();
            app.UseAizenBackgroundJob();
        }

        // Worker spesifik Route konfigürasyonu
        if (this.AppInfo.TypeInclude.Contains(AppType.Worker))
        {
            app.Map("/events", eventUIApp =>
            {
                eventUIApp.UseRouting();
                eventUIApp.UseEndpoints(builder =>
                {
                    builder.MapRazorPages();
                    builder.MapControllerRoute(
                        name: "dynamicForm",
                        pattern: "{controller=DynamicForm}/{action=Index}/{id?}");
                });
            });
        }

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();

        app.UseDeviceInfoMiddleware();
        app.UseClientInfoMiddleware();
        app.UseUserInfoMiddleware();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }

}