namespace Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin;

public sealed class OtpLoginOptions
{
    public const string SectionName = "OtpLogin";

    public int OtpLength { get; set; } = 6;
    public int OtpTtlSeconds { get; set; } = 300;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 5;
    public int MaxRequestsPerIdentifierPerWindow { get; set; } = 5;
    public int IdentifierWindowSeconds { get; set; } = 3600;
}
