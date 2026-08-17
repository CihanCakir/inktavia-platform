namespace Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin;

public sealed class OtpLoginTicketOptions
{
    public const string SectionName = "OtpLoginTicket";

    public string Secret { get; set; } = string.Empty;
    public string ConsumeSecret { get; set; } = string.Empty;
    public int TtlSeconds { get; set; } = 120;
    public string KeycloakAuthorizeBaseUrl { get; set; } = string.Empty;
    public string SpaRedirectUri { get; set; } = string.Empty;
}
