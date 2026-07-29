using Aizen.Modules.Payment.Application.Commands.CreateCustomerDiscountRule;
using Aizen.Modules.Payment.Application.Commands.DeactivateCustomerDiscountRule;
using Aizen.Modules.Payment.Application.Commands.UpdateCustomerDiscountRule;
using Aizen.Modules.Payment.Application.Queries.ResolveCustomerDiscount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/customer-discounts")]
public sealed class CustomerDiscountRuleController : ControllerBase
{
    private readonly ISender _sender;
    public CustomerDiscountRuleController(ISender sender) => _sender = sender;

    /// <summary>Resolves the applicable discount + funding split + budget remaining (P5-engine inputs).</summary>
    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve(
        [FromQuery] long?    customerPlanId,
        [FromQuery] string?  categoryCode,
        [FromQuery] string   currencyCode = "TRY",
        [FromQuery] decimal  serviceBaseAmount = 0m,
        [FromQuery] bool     providerConsent = false,
        [FromQuery] long?    participantPlanSubscriptionId = null,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new ResolveCustomerDiscountQuery
        {
            CustomerPlanId                = customerPlanId,
            CategoryCode                  = categoryCode,
            CurrencyCode                  = currencyCode,
            ServiceBaseAmount             = serviceBaseAmount,
            ProviderConsent               = providerConsent,
            ParticipantPlanSubscriptionId = participantPlanSubscriptionId,
        }, ct));

    [HttpPost("rules")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerDiscountRuleCommand command, CancellationToken ct = default)
        => Ok(await _sender.Send(command, ct));

    [HttpPut("rules/{id:long}")]
    public async Task<IActionResult> Update(
        long id, [FromBody] UpdateCustomerDiscountRuleCommand command, CancellationToken ct = default)
    {
        if (id != command.Id) return BadRequest("Route id and body id must match.");
        return Ok(await _sender.Send(command, ct));
    }

    [HttpPost("rules/{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct = default)
        => Ok(await _sender.Send(new DeactivateCustomerDiscountRuleCommand { Id = id }, ct));
}
