using Aizen.Modules.Payment.Application.Queries.GetProviderPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Route("api/v1/payment/provider-plans")]
public sealed class ProviderPlanController : ControllerBase
{
    private readonly ISender _sender;
    public ProviderPlanController(ISender sender) => _sender = sender;

    [HttpGet]
    [AllowAnonymous] // Plans are publicly visible for marketing/pricing pages
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetProviderPlansQuery(), ct);
        return Ok(result);
    }
}
