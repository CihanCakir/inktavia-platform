using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Warnings;
using Aizen.Bff.MarineProvider.Application.Contracts.Me;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Me;

public sealed class GetProviderStatusQueryHandler
    : AizenQueryHandler<GetProviderStatusQuery, GetProviderStatusResponse>
{
    private readonly IProviderContext _context;
    private readonly IProviderProfileResolver _resolver;
    private readonly IIdentityRemoteCall _identity;
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly ILogger<GetProviderStatusQueryHandler> _logger;

    public GetProviderStatusQueryHandler(
        IProviderContext context,
        IProviderProfileResolver resolver,
        IIdentityRemoteCall identity,
        ICargoDryRemoteCall cargoDry,
        ILogger<GetProviderStatusQueryHandler> logger)
    {
        _context = context;
        _resolver = resolver;
        _identity = identity;
        _cargoDry = cargoDry;
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

                // Capabilities only mean anything for a provider who can actually enter the workspace, and this
                // call sits on the workspace-entry path — so it is scoped to exactly that case and no other.
                await ResolveCapabilitiesAsync(response);
            }
            else if (isApproved)
            {
                // Decided, but the profile is not Active yet (provisioning still in flight, or an approval that
                // failed to activate). Reporting "under review" here is a lie the provider cannot act on — they
                // sit on the provisioning screen forever watching a message that will never change. Say what is
                // actually true.
                response.RequiredNextStep = "Provisioning";
                response.Message = "Your application was approved. We are activating your workspace.";
                _logger.LogWarning(
                    "Profile {ProfileId} is Approved but its status is {ProfileStatus}; the provider cannot enter the workspace.",
                    profileId, dto.Status);
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

    /// <summary>
    /// PROV-MVP-002/003 — publish what this provider is entitled to.
    ///
    /// FAIL CLOSED, DO NOT BREAK. Four route guards run on this response; a CargoDry outage must never keep a
    /// provider out of their workspace. So a failure attaches a Warning and leaves the capability ABSENT: the
    /// SPA hides the gated surfaces (correct — it cannot prove entitlement) while everything else carries on.
    /// The reverse mistake, defaulting to "granted" on error, is how an entitlement check becomes decorative.
    ///
    /// This is NOT the enforcement point. `CargoDryParticipant` re-checks on every CargoDry request here, and
    /// the CargoDry module re-checks again on every provider write.
    /// </summary>
    private async Task ResolveCapabilitiesAsync(GetProviderStatusResponse response)
    {
        try
        {
            var participation = await _cargoDry.GetProviderParticipation();
            if (participation?.Body?.IsParticipant == true)
                response.Capabilities.Add(ProviderCapabilityNames.CargoDry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CargoDry participation lookup failed; the capability is withheld.");
            response.Warnings.Add(ProviderBffWarning.CallFailed("CargoDry.GetParticipation", ex.GetType().Name));
        }
    }
}
