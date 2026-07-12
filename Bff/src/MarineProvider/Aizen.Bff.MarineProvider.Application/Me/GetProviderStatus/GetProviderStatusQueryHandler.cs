using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Warnings;
using Aizen.Bff.MarineProvider.Application.Contracts.Me;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Me.GetProviderStatus;

public sealed class GetProviderStatusQueryHandler
    : AizenQueryHandler<GetProviderStatusQuery, GetProviderStatusResponse>
{
    private readonly IProviderContext _context;
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<GetProviderStatusQueryHandler> _logger;

    public GetProviderStatusQueryHandler(
        IProviderContext context,
        IProviderProfileResolver resolver,
        IProviderIdentityRemoteCall identity,
        ILogger<GetProviderStatusQueryHandler> logger)
    {
        _context = context;
        _resolver = resolver;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<GetProviderStatusResponse?> Handle(
        GetProviderStatusQuery request, CancellationToken cancellationToken)
    {
        var response = new GetProviderStatusResponse
        {
            KeycloakSubject = _context.KeycloakSubject,
            Email = _context.Email,
            EmailVerified = _context.EmailVerified,
            ProviderProfileId = _context.ProviderProfileId
        };

        if (!_context.EmailVerified)
        {
            response.RequiredNextStep = "VerifyEmail";
            response.Message = "Please verify your email to continue.";
        }

        try
        {
            var resolution = await _resolver.ResolveAsync(cancellationToken);
            var dto = resolution.Profile;

            if (resolution.ProfileId is not { } profileId || profileId <= 0 || dto is null)
            {
                response.RequiredNextStep = string.IsNullOrEmpty(response.RequiredNextStep)
                    ? "CompleteProfileLink"
                    : response.RequiredNextStep;
                response.Message = string.IsNullOrEmpty(response.Message)
                    ? "Your account is not linked to a provider profile yet."
                    : response.Message;
                return response;
            }

            response.ProviderProfileId = profileId;
            response.ApprovalStatus = dto.ApprovalStatus;
            response.ProfileStatus = dto.Status;
            response.CompanyName = dto.OrganizationName;
            response.OwnerName = string.Join(" ", new[] { dto.FirstName, dto.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var isApproved = string.Equals(dto.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase);
            var isRejected = string.Equals(dto.ApprovalStatus, "Rejected", StringComparison.OrdinalIgnoreCase);
            var isActive = string.Equals(dto.Status, "Active", StringComparison.OrdinalIgnoreCase);
            var isSuspended = string.Equals(dto.Status, "Suspended", StringComparison.OrdinalIgnoreCase);

            response.CanEnterWorkspace = isApproved && isActive;

            if (!_context.EmailVerified)
            {
                response.RequiredNextStep = "VerifyEmail";
                response.Message = "Please verify your email to continue.";
            }
            else if (isSuspended)
            {
                response.RequiredNextStep = "Suspended";
                response.Message = "Your account is suspended. Please contact support.";
            }
            else if (isRejected)
            {
                response.RequiredNextStep = "Rejected";
                response.Message = "Your application was not approved.";
            }
            else if (response.CanEnterWorkspace)
            {
                response.RequiredNextStep = "EnterWorkspace";
                response.Message = "Your provider account is active.";
            }
            else
            {
                // Fetch onboarding status to distinguish "completing application" from "submitted, awaiting review"
                string? onboardingStatus = null;
                try
                {
                    var onboarding = await _identity.GetProviderOnboarding(profileId);
                    onboardingStatus = onboarding?.Body?.Status;
                    response.OnboardingStatus = onboardingStatus;
                }
                catch (Exception obEx)
                {
                    _logger.LogWarning(obEx, "Onboarding lookup failed for profile {ProfileId}; degrading to AwaitApproval.", profileId);
                    response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.GetOnboarding", obEx.GetType().Name));
                }

                if (string.Equals(onboardingStatus, "NeedsRevision", StringComparison.OrdinalIgnoreCase))
                {
                    response.RequiredNextStep = "NeedsRevision";
                    response.Message = "Your application needs revision. Please update the flagged sections.";
                }
                else if (string.Equals(onboardingStatus, "Submitted", StringComparison.OrdinalIgnoreCase))
                {
                    response.RequiredNextStep = "AwaitApproval";
                    response.Message = "Your application is under review.";
                }
                else
                {
                    response.RequiredNextStep = "CompleteOnboarding";
                    response.Message = "Please complete your application.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider status resolution failed.");
            response.RequiredNextStep = "Unavailable";
            response.Message = "Provider status is temporarily unavailable.";
            response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.GetOrganizerProfile", ex.GetType().Name));
        }

        return response;
    }
}
