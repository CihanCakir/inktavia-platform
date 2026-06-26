namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;

// ── Admin Request DTOs ────────────────────────────────────────────────────────

[DocumentationInfo("Generate batch BFF request", "Admin request to generate a new CargoDry kit batch.")]
public sealed class GenerateBatchBffRequest
{
    public string ProductCode { get; init; } = default!;
    public int    Count       { get; init; }
}

[DocumentationInfo("Revoke kit BFF request", "Admin request to revoke a CargoDry kit with a reason.")]
public sealed class RevokeKitBffRequest
{
    public string Reason { get; init; } = default!;
}

[DocumentationInfo("Extend kit BFF request", "Admin request to extend kit validity by adding days.")]
public sealed class ExtendKitBffRequest
{
    public int AddedDays { get; init; }
}

[DocumentationInfo("Renew kit BFF request", "Admin request to renew an expiring kit.")]
public sealed class RenewKitBffRequest
{
    public int     AddedDays  { get; init; }
    public string? PaymentRef { get; init; }
}

// ── Onboarding Request DTOs ───────────────────────────────────────────────────

[DocumentationInfo("Validate kit BFF request", "Public request to validate a CargoDry kit serial/QR before activation.")]
public sealed class ValidateKitBffRequest
{
    public string  SerialNumber { get; init; } = default!;
    public string  BatchCode    { get; init; } = default!;
    public string? Signature    { get; init; }
}

[DocumentationInfo("Activate kit BFF request", "Authenticated user request to activate a CargoDry kit on a vessel.")]
public sealed class ActivateKitBffRequest
{
    public string ActivationToken { get; init; } = default!;
    public long   VesselId        { get; init; }
}
