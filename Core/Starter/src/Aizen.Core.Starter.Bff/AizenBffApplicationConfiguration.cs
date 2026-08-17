using Aizen.Core.Api.Middleware;
using Aizen.Core.InfoAccessor.Extensions.UserInfo;
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
        // Populates IAizenUserInfoAccessor.UserInfo from X-Aizen-User-Token header.
        // Must run after routing but before controllers so that the delegating handler
        // can forward the identity JWT to downstream microservices.
        app.UseUserInfoMiddleware();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}