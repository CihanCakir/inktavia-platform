using Aizen.Core.Api.Middleware;
using Aizen.Core.Starter.Abstraction;
using Aizen.Core.Starter.Abstraction.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Aizen.Core.Starter.Api;

public class AizenBffApplicationConfiguration : IAizenApplicationConfiguration
{
    public void Configure(AizenApplication app, IWebHostEnvironment env)
    {
        app.UseMiddleware<AizenErrorHandlingMiddleware>();
        app.UseMiddleware<AizenRequestResponseMiddleware>();
        app.UseMiddleware<AizenInfoMiddleware>();
        app.UseAizenGlobalExceptionMiddleware();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
    }
}