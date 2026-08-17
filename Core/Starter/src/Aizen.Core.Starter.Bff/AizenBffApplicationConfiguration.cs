using Aizen.Core.Api.Middleware;
using Aizen.Core.InfoAccessor.Extensions.UserInfo;
using Aizen.Core.Starter.Abstraction;
using Aizen.Core.Starter.Abstraction.Middleware;
using Aizen.Core.Starter.Bff;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        // ── CORS — must run BEFORE authentication/authorization ───────────────────────────────────────────
        // A CORS preflight is an OPTIONS request the browser sends WITHOUT an Authorization header (it cannot
        // send one, by spec). If CORS ran after UseAuthorization(), that preflight would hit an [Authorize]
        // endpoint — a SignalR hub, for instance — and be answered with 401 before the CORS middleware ever
        // saw it. The browser then abandons the real request, and the endpoint looks unreachable while every
        // server-side setting appears correct.
        //
        // This is exactly what happened to /hubs/provider/negotiate: a BFF that called UseCors() after
        // Build() had its CORS middleware appended *behind* this pipeline. Controller routes survived only
        // because their preflights match no endpoint and so slip past authorization untouched.
        //
        // Applied only when the BFF registered the policy; a BFF with no SPA in front of it is unaffected.
        var corsOptions = app.ApplicationServices.GetService<IOptions<CorsOptions>>()?.Value;
        if (corsOptions?.GetPolicy(AizenBffCors.PolicyName) is not null)
            app.UseCors(AizenBffCors.PolicyName);

        // Populates IAizenUserInfoAccessor.UserInfo from X-Aizen-User-Token header.
        // Must run after routing but before controllers so that the delegating handler
        // can forward the identity JWT to downstream microservices.
        app.UseUserInfoMiddleware();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}