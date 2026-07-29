using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.RejectSubMerchant;

public sealed class RejectSubMerchantBffCommand : AizenCommand<SubMerchantOnboardingMutateBffResponse>
{
    public long    ProviderProfileId { get; init; }
    public string? Reason            { get; init; }
}
