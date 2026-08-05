namespace Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

/// <summary>Server-to-server: mint a single-use login_ticket for an already-authenticated Keycloak subject
/// (password/social sign-in, where there is no OTP step). IdentityWrite-only.</summary>
public sealed class MintParticipantTicketRequest
{
    public string Sub { get; set; } = default!;
}

public sealed class MintParticipantTicketResponse
{
    public string LoginTicket { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}
