using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.VerifySubMerchant;

public sealed class VerifySubMerchantBffCommand : AizenCommand<SubMerchantOnboardingMutateBffResponse>
{
    public long ProviderProfileId { get; init; }
}
