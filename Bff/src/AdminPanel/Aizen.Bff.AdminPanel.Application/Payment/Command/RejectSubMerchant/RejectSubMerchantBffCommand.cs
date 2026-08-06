using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.RejectSubMerchant;

public sealed class RejectSubMerchantBffCommand : AizenCommand<SubMerchantOnboardingMutateBffResponse>
{
    public long    ProviderProfileId { get; init; }
    public string? Reason            { get; init; }
}
