using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;
using Aizen.Bff.MarineProvider.Application.Onboarding.Documents;
using Aizen.Bff.MarineProvider.Application.Onboarding.GetOnboarding;
using Aizen.Bff.MarineProvider.Application.Onboarding.SaveOnboardingStep;
using Aizen.Bff.MarineProvider.Application.Onboarding.SubmitOnboarding;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1.Onboarding;

[ApiController]
[Route("api/v1/provider/onboarding")]
[Tags("Provider - Onboarding")]
[Authorize]
public sealed class ProviderOnboardingController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderOnboardingController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) { _cqrs = cqrs; }

    [HttpGet]
    [ProducesResponseType(typeof(OnboardingResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OnboardingResponse>> Get(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetOnboardingQuery(), ct);
        return SetResponse(result);
    }

    [HttpPut("steps/{step}")]
    [ProducesResponseType(typeof(SaveOnboardingStepResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SaveOnboardingStepResponse>> SaveStep(
        string step, [FromBody] SaveOnboardingStepRequest request, CancellationToken ct)
    {
        var command = new SaveOnboardingStepCommand
        {
            Step = step,
            StepStatus = request.StepStatus,
            StepData = request.StepData,
            SchemaVersion = request.SchemaVersion,
        };
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("submit")]
    [ProducesResponseType(typeof(SubmitOnboardingResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubmitOnboardingResponse>> Submit(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new SubmitOnboardingCommand(), ct);
        return SetResponse(result);
    }

    [HttpPost("documents")]
    [ProducesResponseType(typeof(AttachDocumentBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AttachDocumentBffResponse>> AttachDocument(
        [FromBody] AttachDocumentBffRequest request, CancellationToken ct)
    {
        var command = new AttachOnboardingDocumentCommand
        {
            FileId = request.FileId,
            DocumentType = request.DocumentType,
            Issuer = request.Issuer,
        };
        return SetResponse(await _cqrs.ProcessAsync(command, ct));
    }

    [HttpDelete("documents/{fileId:guid}")]
    [ProducesResponseType(typeof(DeleteDocumentBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DeleteDocumentBffResponse>> DeleteDocument(Guid fileId, CancellationToken ct)
    {
        var command = new DeleteOnboardingDocumentCommand { FileId = fileId };
        return SetResponse(await _cqrs.ProcessAsync(command, ct));
    }

    [HttpPost("documents/{fileId:guid}/access-url")]
    [ProducesResponseType(typeof(DocumentAccessUrlBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DocumentAccessUrlBffResponse>> GetDocumentAccessUrl(Guid fileId, CancellationToken ct)
    {
        var command = new GetDocumentAccessUrlCommand { FileId = fileId };
        return SetResponse(await _cqrs.ProcessAsync(command, ct));
    }
}
