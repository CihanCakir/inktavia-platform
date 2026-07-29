using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetSubMerchantOnboardingQueue;

public sealed class GetSubMerchantOnboardingQueueBffQuery : AizenQuery<GetSubMerchantOnboardingQueueBffResponse>
{
    public ProviderSubMerchantOnboardingStatus? Status { get; init; }
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
