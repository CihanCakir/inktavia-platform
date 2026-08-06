using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateCustomerDiscountRule;


// ─── CustomerDiscountRule: Update ────────────────────────────────────────────
public sealed class UpdateCustomerDiscountRuleBffCommand : AizenCommand<UpdateCustomerDiscountRuleBffResponse>
{
    public long                                 Id   { get; init; }
    public UpdateCustomerDiscountRuleBffRequest Body { get; init; } = default!;
}
public sealed class UpdateCustomerDiscountRuleBffResponse { public CustomerDiscountRuleMutateBffResult Result { get; init; } = default!; }
