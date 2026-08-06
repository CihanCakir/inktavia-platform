using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateCustomerDiscountRule;


// ─── CustomerDiscountRule: Deactivate ────────────────────────────────────────
public sealed class DeactivateCustomerDiscountRuleBffCommand : AizenCommand<DeactivateCustomerDiscountRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivateCustomerDiscountRuleBffResponse { public CustomerDiscountRuleMutateBffResult Result { get; init; } = default!; }
