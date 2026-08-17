using System.Text.Json;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.SaveProviderOnboardingStep;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.SubmitProviderOnboarding;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.RequestProviderOnboardingRevision;
using Aizen.Modules.InktaviaStore.Application.Identity.Query.Onboarding.GetProviderOnboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity;

[ApiController]
[Route("api/v1/identity/provider-onboarding")]
[Tags("Identity - Provider Onboarding")]
[Authorize(Policy = "IdentityWrite")]
public sealed class ProviderOnboardingController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _sender;

    public ProviderOnboardingController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor) { _sender = cqrsProcessor; }

    [HttpGet("{profileId:long}")]
    [ProducesResponseType(typeof(ProviderOnboardingResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderOnboardingResponse>> Get(long profileId, CancellationToken ct)
    {
        var query = new GetProviderOnboardingQuery { ProfileId = profileId };
        var result = await _sender.ProcessAsync(query, ct);
        return SetResponse(result);
    }

    [HttpPut("{profileId:long}/steps/{step}")]
    [ProducesResponseType(typeof(SaveProviderOnboardingStepResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SaveProviderOnboardingStepResponse>> SaveStep(
        long profileId, string step,
        [FromBody] SaveProviderOnboardingStepRequest request, CancellationToken ct)
    {
        var command = new SaveProviderOnboardingStepCommand
        {
            ProfileId = profileId,
            Step = step,
            StepStatus = request.StepStatus,
            // Fail closed. The previous version fell back to "{}" when the payload did not bind, which meant a
            // step was persisted EMPTY and reported as saved — silent data loss. A step with no data is a bug.
            StepDataJson = !string.IsNullOrWhiteSpace(request.StepDataJson)
                ? request.StepDataJson
                : throw new Aizen.Core.Infrastructure.Exception.AizenBusinessException("Step data is required."),
            SchemaVersion = request.SchemaVersion,
        };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("{profileId:long}/submit")]
    [ProducesResponseType(typeof(SubmitProviderOnboardingResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubmitProviderOnboardingResponse>> Submit(long profileId, CancellationToken ct)
    {
        var command = new SubmitProviderOnboardingCommand { ProfileId = profileId };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("{profileId:long}/revision")]
    [ProducesResponseType(typeof(RequestProviderOnboardingRevisionResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RequestProviderOnboardingRevisionResponse>> RequestRevision(
        long profileId,
        [FromBody] RequestProviderOnboardingRevisionRequest request, CancellationToken ct)
    {
        var command = new RequestProviderOnboardingRevisionCommand
        {
            ProfileId = profileId,
            Steps = request.Steps,
            Note = request.Note,
        };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }
}
