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

/// <summary>Server-to-server: resolve a login identifier (email|phone) to the canonical participant email
/// (= Keycloak username) + subject, WITHOUT sending an OTP. Used by phone password-login. IdentityWrite-only.</summary>
public sealed class ResolveParticipantIdentifierRequest
{
    public string Channel { get; set; } = default!;    // "email" | "phone"
    public string Identifier { get; set; } = default!;
}

public sealed class ResolveParticipantIdentifierResponse
{
    public bool Found { get; set; }
    public string? Email { get; set; }
    public string? KeycloakSubjectId { get; set; }
}
