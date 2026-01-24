using Aizen.Core.InfoAccessor.Extensions.ClientInfo;
using Aizen.Core.InfoAccessor.Extensions.DeviceInfo;
using Aizen.Core.Scheduler.Extensions;
using Aizen.Core.Starter.Abstraction;
using Aizen.Core.Starter.Abstraction.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

namespace Aizen.Core.Starter.Scheduler;

public class AizenSchedulerApplicationConfiguration : IAizenApplicationConfiguration
{
    public void Configure(AizenApplication app, IWebHostEnvironment env)
    {
        app.UseMiddleware<AizenInfoMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();


        app.UseDeviceInfoMiddleware();
        app.UseClientInfoMiddleware();


        app.UseAizenRecurringJob();
        app.UseAizenBackgroundJob();

        app.UseAuthorization();
        app.MapControllers();
    }
}