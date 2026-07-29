using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.VerifySubMerchant;

public sealed class VerifySubMerchantBffCommand : AizenCommand<SubMerchantOnboardingMutateBffResponse>
{
    public long ProviderProfileId { get; init; }
}
