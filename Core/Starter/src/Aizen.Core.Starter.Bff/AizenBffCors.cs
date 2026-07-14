namespace Aizen.Core.Starter.Bff;

/// <summary>
/// Shared CORS policy name for BFFs that a browser SPA talks to directly.
///
/// A BFF opts in by registering the policy under this name:
///
///     builder.Services.AddCors(o => o.AddPolicy(AizenBffCors.PolicyName, p => p
///         .WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
///
/// The BFF pipeline (<see cref="Aizen.Core.Starter.Api.AizenBffApplicationConfiguration"/>) then applies it
/// **before** authentication/authorization. That ordering is not cosmetic:
///
/// A CORS preflight is an `OPTIONS` request that the browser sends **without** an `Authorization` header — it
/// cannot send one, by spec. If the CORS middleware sits behind `UseAuthorization()`, that preflight hits an
/// `[Authorize]` endpoint (a SignalR hub, for example), gets a 401, and the browser abandons the real request.
/// The endpoint then looks unreachable while every server-side config appears correct.
///
/// A BFF that does not register the policy is unaffected — the pipeline skips CORS entirely.
/// </summary>
public static class AizenBffCors
{
    public const string PolicyName = "aizen-bff-web";
}
