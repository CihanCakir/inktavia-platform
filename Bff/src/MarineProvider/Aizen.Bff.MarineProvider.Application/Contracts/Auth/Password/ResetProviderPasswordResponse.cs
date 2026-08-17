namespace Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;

/// <summary>Result of the password reset. On success the reset token is consumed and Keycloak sessions revoked.</summary>
public sealed class ResetProviderPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
