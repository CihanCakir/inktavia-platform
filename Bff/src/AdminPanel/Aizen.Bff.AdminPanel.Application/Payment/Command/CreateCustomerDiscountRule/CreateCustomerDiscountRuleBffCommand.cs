using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateCustomerDiscountRule;


// ─── CustomerDiscountRule: Create ────────────────────────────────────────────
public sealed class CreateCustomerDiscountRuleBffCommand : AizenCommand<CreateCustomerDiscountRuleBffResponse>
{
    public CreateCustomerDiscountRuleBffRequest Body { get; init; } = default!;
}
public sealed class CreateCustomerDiscountRuleBffResponse { public CustomerDiscountRuleCreateBffResult Result { get; init; } = default!; }
