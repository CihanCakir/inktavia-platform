namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth;

/// <summary>Password register/login/refresh/logout contracts. Register creates the account; every
/// BFF-issued session (OTP, password, social) is minted the same way → an inktavia-mobile token.</summary>

// ── Requests (mobile app → BFF) ───────────────────────────────────────────────

public sealed class MobileRegisterRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
}

public sealed class MobileLoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public sealed class MobileRefreshRequest
{
    public string RefreshToken { get; set; } = default!;
}

public sealed class MobileLogoutRequest
{
    public string RefreshToken { get; set; } = default!;
}

/// <summary>Native Google sign-in: the app posts the Google SDK id_token.</summary>
public sealed class MobileGoogleLoginRequest
{
    public string IdToken { get; set; } = default!;
}

/// <summary>Native Apple sign-in: the app posts the Apple SDK identityToken (+ fullName on first auth only).</summary>
public sealed class MobileAppleLoginRequest
{
    public string IdentityToken { get; set; } = default!;
    public string? FullName { get; set; }
}

// ── Responses (BFF → mobile app) ──────────────────────────────────────────────

/// <summary>Real Keycloak tokens (inktavia-mobile), identical shape across OTP/password/refresh.</summary>
public sealed class MobileAuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public sealed class MobileLogoutResponse
{
    public bool Success { get; set; } = true;
}
