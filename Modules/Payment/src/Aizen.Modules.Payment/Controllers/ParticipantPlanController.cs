using Aizen.Modules.Payment.Application.Queries.GetParticipantPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

[ApiController]
[Route("api/v1/payment/participant-plans")]
public sealed class ParticipantPlanController : ControllerBase
{
    private readonly ISender _sender;
    public ParticipantPlanController(ISender sender) => _sender = sender;

    [HttpGet]
    [AllowAnonymous] // Plans are publicly visible
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetParticipantPlansQuery(), ct);
        return Ok(result);
    }
}
