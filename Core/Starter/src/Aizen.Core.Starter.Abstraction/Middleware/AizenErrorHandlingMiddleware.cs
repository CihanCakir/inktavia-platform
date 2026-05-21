using System.Net;
using System.Text.Json;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception.Middleware;
using Microsoft.AspNetCore.Http;


namespace Aizen.Core.Starter.Abstraction.Middleware
{
    public class AizenErrorHandlingMiddleware
    {
         private readonly RequestDelegate _next;

    public AizenErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

            var header = AizenResponseHeader.Fail(ex);
            var response = new AizenApiResponse<NoContext>(header, new NoContext());
            var camelCaseJsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, camelCaseJsonSerializerOptions));

        }
    }
    }
}