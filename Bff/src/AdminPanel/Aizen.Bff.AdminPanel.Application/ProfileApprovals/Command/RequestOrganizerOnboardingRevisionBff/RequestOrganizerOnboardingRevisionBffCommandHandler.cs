using Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.ProfileApprovals.Command;

[DocumentationInfo("Request organizer onboarding revision BFF command handler",
    "Validates input and calls Identity provider-onboarding revision endpoint so the provider is prompted to fix specific steps.")]
public sealed class RequestOrganizerOnboardingRevisionBffCommandHandler
    : AizenCommandHandler<RequestOrganizerOnboardingRevisionBffCommand, OnboardingRevisionBffResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<RequestOrganizerOnboardingRevisionBffCommandHandler> _logger;

    public RequestOrganizerOnboardingRevisionBffCommandHandler(
        IIdentityRemoteCall identity,
        ILogger<RequestOrganizerOnboardingRevisionBffCommandHandler> logger)
    {
        _identity = identity;
        _logger   = logger;
    }

    public override async Task<OnboardingRevisionBffResponse?> Handle(
        RequestOrganizerOnboardingRevisionBffCommand request, CancellationToken cancellationToken)
    {
        var response = new OnboardingRevisionBffResponse();

        // ── Validate ────────────────────────────────────────────────────────
        if (request.Steps is null || request.Steps.Length == 0)
        {
            response.Warnings.Add(new AdminBffWarning("Validation", "At least one step must be selected for revision."));
            return response;
        }

        var note = request.Note?.Trim() ?? string.Empty;
        if (note.Length < 10 || note.Length > 2000)
        {
            response.Warnings.Add(new AdminBffWarning("Validation", "Revision note must be between 10 and 2000 characters."));
            return response;
        }

        // ── Call Identity ────────────────────────────────────────────────────
        try
        {
            var result = await _identity.RequestProviderOnboardingRevisionAdmin(
                request.ProfileId,
                new RequestProviderOnboardingRevisionRequest
                {
                    Steps = request.Steps,
                    Note  = note
                });

            if (result?.Header?.IsSuccess != true)
            {
                _logger.LogWarning(
                    "[OnboardingRevisionBff] Identity call failed: profileId={ProfileId} errorCode={Code} errorMsg={Msg}",
                    request.ProfileId, result?.Header?.ErrorCode, result?.Header?.ErrorMessage);

                response.Warnings.Add(AdminBffWarning.CallFailed(
                    "Identity", result?.Header?.ErrorMessage ?? "Could not request onboarding revision."));
                return response;
            }

            _logger.LogInformation(
                "[OnboardingRevisionBff] Revision requested: userId={UserId} profileId={ProfileId} steps=[{Steps}]",
                request.UserId, request.ProfileId, string.Join(",", request.Steps));

            response.Success = true;
            response.Message = "Onboarding revision request sent successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[OnboardingRevisionBff] Exception calling Identity: profileId={ProfileId}", request.ProfileId);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }
}
