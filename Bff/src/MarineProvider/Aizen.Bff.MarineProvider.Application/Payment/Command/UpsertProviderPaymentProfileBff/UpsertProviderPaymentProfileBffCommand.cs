using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Payment;

public sealed class UpsertProviderPaymentProfileBffCommand : AizenCommand<ProviderPaymentProfileDto>
{
    public string  Iban      { get; init; } = default!;
    public string? LegalName { get; init; }
    public string? TaxNumber { get; init; }

    // ── BE-I1 type-varied KYC (additive, optional — legacy body stays valid) ──
    public string? SubMerchantType   { get; init; }
    public string? IdentityNumber    { get; init; }
    public string? TaxOffice         { get; init; }
    public string? LegalCompanyTitle { get; init; }
}
