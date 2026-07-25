namespace Aizen.Modules.Payment.Abstraction.Dto;

public sealed class ProviderPaymentProfileDto
{
    public string  GatewayProvider { get; init; } = default!;
    public bool    HasIban         { get; init; }
    public string? IbanMasked      { get; init; }
    public string? LegalName       { get; init; }
    public string? TaxNumberMasked { get; init; }
    public string  Status          { get; init; } = "OnHold";
    public DateTimeOffset? VerifiedAt { get; init; }
}
