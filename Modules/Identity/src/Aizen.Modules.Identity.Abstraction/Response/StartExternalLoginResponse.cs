namespace Aizen.Modules.Identity.Abstraction.Response
{
    public sealed record StartExternalLoginResponse(string AuthorizationUrl, string State);
}