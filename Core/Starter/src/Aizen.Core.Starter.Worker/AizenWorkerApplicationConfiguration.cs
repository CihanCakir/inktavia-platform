using Aizen.Core.Starter.Abstraction;
using Aizen.Core.Starter.Abstraction.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Aizen.Core.Starter.Worker;

public class AizenWorkerApplicationConfiguration : IAizenApplicationConfiguration
{
    public void Configure(AizenApplication app, IWebHostEnvironment env)
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
        app.UseMiddleware<AizenInfoMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
    }
}