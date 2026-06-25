namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class RevokeKitResponse
{
    public long            KitId        { get; init; }
    public string          KitCode      { get; init; } = default!;
    public string          SerialNumber { get; init; } = default!;
    public string          Status       { get; init; } = "Revoked";
    public string          Reason       { get; init; } = default!;
    public DateTimeOffset  RevokedAt    { get; init; }
}
