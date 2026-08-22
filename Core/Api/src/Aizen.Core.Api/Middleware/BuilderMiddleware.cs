using System.Diagnostics;
using System.Text.Json;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Infrastructure.Exception.Middleware;
using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Common.Abstraction.Localization;
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
                    // FAZ13A #68: hata mesajı dilini GERÇEKTEN beslenen kanaldan çöz. Eskiden yalnız "Language"
                    // başlığına bakılıyordu; onu kimse göndermiyor → lang hep "TR" kalıyordu. Sıra:
                    //   1) Accept-Language (frontend + Faz 12B'den beri tüm BFF'ler gönderiyor; başlık — kültür
                    //      kodu DEĞİL, o yüzden paylaşılan AcceptLanguageParser ile en yüksek q'lu birincil etiket).
                    //   2) Legacy "Language" başlığı (gönderen bir şey varsa çalışsın).
                    //   3) "TR" (bugünkü varsayılan). Sözlükte olmayan bir dile çözülürse GetErrorMessage zaten
                    //      varsayılan (ilk) girdiye düşer — boş string DEĞİL. Bozuk başlık → parser null → "TR".
                    var lang = ErrorLanguageResolver.Resolve(
                        context.Request.Headers.AcceptLanguage.ToString(),
                        context.Request.Headers["Language"].ToString());

                    var statusCode = exceptionFeature.Error switch
                    {
                        AizenUpstreamException => 502,
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
                    // A downstream/infra failure carries an opaque correlation id that ties this response to
                    // the server-side Error log — echo it (not the real upstream status) so support can trace it.
                    if (exceptionFeature.Error is AizenUpstreamException upstream)
                    {
                        response.Header.ErrorDetails = $"correlationId={upstream.CorrelationId}";
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