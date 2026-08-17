using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderSubMerchantOnboardingQueue;

[DocumentationInfo("Get provider sub-merchant onboarding queue query handler (BE-I1)",
    "Paged admin review queue of provider payment profiles by onboarding status. Masks IBAN/tax/sub-merchant key; " +
    "surfaces the split-eligibility signal so the admin can prioritise verify/reject.")]
public sealed class GetProviderSubMerchantOnboardingQueueQueryHandler
    : AizenQueryHandler<GetProviderSubMerchantOnboardingQueueQuery, ProviderSubMerchantOnboardingQueueDto>
{
    private readonly IProviderPaymentProfileRepository _repo;

    public GetProviderSubMerchantOnboardingQueueQueryHandler(IProviderPaymentProfileRepository repo) => _repo = repo;

    public override async Task<ProviderSubMerchantOnboardingQueueDto?> Handle(
        GetProviderSubMerchantOnboardingQueueQuery request, CancellationToken ct)
    {
        var page     = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var (items, total) = await _repo.GetOnboardingQueueAsync(request.Status, (page - 1) * pageSize, pageSize, ct);

        return new ProviderSubMerchantOnboardingQueueDto
        {
            Total    = total,
            Page     = page,
            PageSize = pageSize,
            Items    = items.Select(e => new ProviderSubMerchantOnboardingQueueItemDto
            {
                ProviderProfileId    = e.ProviderProfileId,
                OnboardingStatus     = e.OnboardingStatus,
                IsSplitEligible      = e.IsSplitEligible,
                HasIban              = !string.IsNullOrEmpty(e.IbanEncrypted),
                LegalName            = e.LegalName,
                TaxNumberMasked      = MaskTaxNumber(e.TaxNumber),
                SubMerchantKeyMasked = MaskSubMerchantKey(e.SubMerchantKey),
                VerifiedAt           = e.VerifiedAt,
                Status               = e.Status,
            }).ToList(),
        };
    }

    private static string? MaskTaxNumber(string? taxNumber)
    {
        if (string.IsNullOrEmpty(taxNumber)) return null;
        return taxNumber.Length <= 3 ? new string('*', taxNumber.Length) : new string('*', taxNumber.Length - 3) + taxNumber[^3..];
    }

    private static string? MaskSubMerchantKey(string? key)
        => string.IsNullOrEmpty(key) ? null : (key.Length <= 4 ? new string('*', key.Length) : $"****{key[^4..]}");
}
