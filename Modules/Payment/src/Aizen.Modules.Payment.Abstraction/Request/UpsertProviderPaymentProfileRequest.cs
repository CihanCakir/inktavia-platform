namespace Aizen.Modules.Payment.Abstraction.Request;

public sealed class UpsertProviderPaymentProfileRequest
{
    public string  Iban       { get; set; } = default!;
    public string? LegalName  { get; set; }
    public string? TaxNumber  { get; set; }
}
