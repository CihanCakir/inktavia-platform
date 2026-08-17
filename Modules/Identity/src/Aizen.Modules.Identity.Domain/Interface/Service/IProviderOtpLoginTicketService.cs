namespace Aizen.Modules.Identity.Domain.Interface.Service;

public interface IProviderOtpLoginTicketService
{
    Task<OtpLoginTicketResult> MintAsync(string keycloakSubjectId, string clientId, CancellationToken ct);
    Task<OtpLoginConsumeResult> ConsumeAsync(string jti, CancellationToken ct);
}

public sealed class OtpLoginTicketResult
{
    public string LoginTicket { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public sealed class OtpLoginConsumeResult
{
    public bool Consumed { get; set; }
    public string? Sub { get; set; }
}
