using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Aizen.Core.Starter.Abstraction.Middleware;

public class AizenInfoMiddleware
{
    private readonly RequestDelegate _next;

    public AizenInfoMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/info")
        {
            var infoAccessor = context.RequestServices.GetRequiredService<IAizenInfoAccessor>();
            var info = new
            {
                infoAccessor.ServerInfoAccessor.ServerInfo,
                infoAccessor.NetworkInfoAccessor.NetworkInfo,
                infoAccessor.AppInfoAccessor.AppInfo
            };

            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new StringEnumConverter());
            var json = JsonConvert.SerializeObject(info, Formatting.Indented, jsonSettings);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        else
        {
            await _next(context);
        }
    }
}