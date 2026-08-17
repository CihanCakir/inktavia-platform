using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Commands.UpsertProviderPaymentProfile;

public sealed class UpsertProviderPaymentProfileCommand : AizenCommand<ProviderPaymentProfileDto>
{
    public long    ProviderProfileId { get; init; }
    public string  Iban              { get; init; } = default!;
    public string? LegalName         { get; init; }
    public string? TaxNumber         { get; init; }

    // ── BE-I1 type-varied KYC (additive, optional) ──
    public string? SubMerchantType   { get; init; }
    public string? IdentityNumber    { get; init; }
    public string? TaxOffice         { get; init; }
    public string? LegalCompanyTitle { get; init; }
}
