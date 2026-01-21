using System.Diagnostics;
using System.Text.Json;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Infrastructure.Exception.Middleware;
using Aizen.Core.Common.Abstraction.Exception;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Aizen.Core.Api.Middleware
{
    public static class BuilderMiddleware
    {
        public static void UseAizenGlobalExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(config =>
            {
                config.Run(async context =>
                {
                    var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                    var activity = Activity.Current.Start();
                    activity.SetStatus(ActivityStatusCode.Error, exceptionFeature.Error.ToString());
                    var lang = string.IsNullOrWhiteSpace(context.Request.Headers["Language"])
                        ? "TR"
                        : context.Request.Headers["Language"].ToString();

                    var statusCode = exceptionFeature.Error switch
                    {
                        AizenBusinessException => 400,
                        _ => 500
                    };

                    var response =
                        new AizenApiResponse<NoContext>(AizenResponseHeader.Fail(exceptionFeature.Error), null);
                    if (exceptionFeature.Error is AizenException)
                    {
                        var aizenEx = (AizenException)exceptionFeature.Error;
                        response.Header.ErrorMessage = aizenEx.GetErrorMessage(lang);
                    }

                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = statusCode;
                    var jsonString = JsonConvert.SerializeObject(response, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });

                    await context.Response.WriteAsync(jsonString);
                });
            });
        }
    }
}