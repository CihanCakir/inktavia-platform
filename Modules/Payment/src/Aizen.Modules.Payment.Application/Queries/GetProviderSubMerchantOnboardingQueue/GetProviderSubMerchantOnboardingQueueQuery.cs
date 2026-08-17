using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderSubMerchantOnboardingQueue;

/// <summary>BE-I1 — admin sub-merchant onboarding review queue (paged, optional status filter).</summary>
public sealed class GetProviderSubMerchantOnboardingQueueQuery : AizenQuery<ProviderSubMerchantOnboardingQueueDto>
{
    public ProviderSubMerchantOnboardingStatus? Status { get; init; }
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
