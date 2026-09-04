using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Timeout;

namespace Aizen.Core.Infrastructure.Api;

[ApiController]
[Route("api/[controller]")]
public class AizenWebApiController : ControllerBase
{
    protected IHttpContextAccessor ContextAccessor { get; }

    public AizenWebApiController(IHttpContextAccessor httpContextAccessor)
    {
        ContextAccessor = httpContextAccessor;
    }

    protected AizenApiResponse<T> SetResponse<T>(T data) where T : class
    {
        var response = new AizenApiResponse<T>(AizenResponseHeader.Success(), data);
        return response;
    }
    
    protected async Task<AizenApiResponse<T>> ExecuteAsyncWithTimeout<T>(Func<Task<T>> action, int timeoutInMilliseconds, T defaultValue = default)
        where T : class
    {
        var fallbackPolicy = Policy<T>
            .Handle<TimeoutRejectedException>()
            .FallbackAsync(
                fallbackValue: defaultValue,
                onFallbackAsync: async b =>
                {
                    await Task.CompletedTask;
                });

        var timeoutPolicy = Policy.TimeoutAsync<T>(TimeSpan.FromMilliseconds(timeoutInMilliseconds));
        var policyWrap = Policy.WrapAsync(fallbackPolicy, timeoutPolicy);
        var result = await policyWrap.ExecuteAsync(action);

        return SetResponse(result);
    }
}