namespace Aizen.Modules.CargoDry.Abstraction.Interface.Service;

public interface IActivationTokenService
{
    string Generate(string serialNumber);
    ActivationTokenClaims? Verify(string token);
}

public sealed class ActivationTokenClaims
{
    public string SerialNumber { get; init; } = default!;
    public string Jti          { get; init; } = default!;
    public DateTimeOffset ExpiresAt { get; init; }
}
