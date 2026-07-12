using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Onboarding.GetOnboarding;

public sealed class GetOnboardingQueryHandler
    : AizenQueryHandler<GetOnboardingQuery, OnboardingResponse>
{
    private readonly IProviderContext _context;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<GetOnboardingQueryHandler> _logger;

    public GetOnboardingQueryHandler(IProviderContext context, IProviderIdentityRemoteCall identity, ILogger<GetOnboardingQueryHandler> logger)
    { _context = context; _identity = identity; _logger = logger; }

    public override async Task<OnboardingResponse?> Handle(GetOnboardingQuery request, CancellationToken ct)
    {
        var profileId = _context.ProviderProfileId ?? 0;
        if (profileId <= 0) return null;

        try
        {
            var result = await _identity.GetProviderOnboarding(profileId);
            var body = result.Body;
            if (body is null) return null;

            return new OnboardingResponse
            {
                ProfileId = body.ProfileId,
                Status = body.Status,
                SchemaVersion = body.SchemaVersion,
                StepStatuses = body.StepStatuses,
                Draft = body.Draft,
                RevisionSteps = body.RevisionSteps,
                RevisionNote = body.RevisionNote,
                LastSavedAtUtc = body.LastSavedAtUtc,
                SubmittedAtUtc = body.SubmittedAtUtc,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get onboarding for profile {ProfileId}.", profileId);
            return null;
        }
    }
}
