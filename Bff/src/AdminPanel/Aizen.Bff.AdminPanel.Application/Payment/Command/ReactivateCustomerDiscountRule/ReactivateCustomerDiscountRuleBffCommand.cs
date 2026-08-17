using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivateCustomerDiscountRule;


// ─── CustomerDiscountRule: Reactivate ────────────────────────────────────────
public sealed class ReactivateCustomerDiscountRuleBffCommand : AizenCommand<ReactivateCustomerDiscountRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class ReactivateCustomerDiscountRuleBffResponse { public CustomerDiscountRuleMutateBffResult Result { get; init; } = default!; }
