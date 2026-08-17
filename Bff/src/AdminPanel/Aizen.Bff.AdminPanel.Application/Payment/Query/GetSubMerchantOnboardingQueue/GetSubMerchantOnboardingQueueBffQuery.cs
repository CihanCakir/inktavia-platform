using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetSubMerchantOnboardingQueue;

public sealed class GetSubMerchantOnboardingQueueBffQuery : AizenQuery<GetSubMerchantOnboardingQueueBffResponse>
{
    public ProviderSubMerchantOnboardingStatus? Status { get; init; }
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
