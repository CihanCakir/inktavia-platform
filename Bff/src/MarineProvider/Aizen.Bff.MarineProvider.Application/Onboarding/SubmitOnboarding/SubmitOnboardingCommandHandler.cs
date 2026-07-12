using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Onboarding.SubmitOnboarding;

public sealed class SubmitOnboardingCommandHandler
    : AizenCommandHandler<SubmitOnboardingCommand, SubmitOnboardingResponse>
{
    private readonly IProviderContext _context;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<SubmitOnboardingCommandHandler> _logger;

    public SubmitOnboardingCommandHandler(IProviderContext context, IProviderIdentityRemoteCall identity, ILogger<SubmitOnboardingCommandHandler> logger)
    { _context = context; _identity = identity; _logger = logger; }

    public override async Task<SubmitOnboardingResponse?> Handle(SubmitOnboardingCommand request, CancellationToken ct)
    {
        var profileId = _context.ProviderProfileId ?? 0;
        if (profileId <= 0) return new SubmitOnboardingResponse { Success = false, Message = "Provider profile not found." };

        try
        {
            var result = await _identity.SubmitProviderOnboarding(profileId);
            var data = result.Body;
            return data is not null
                ? new SubmitOnboardingResponse { Success = data.Success, Message = data.Message }
                : new SubmitOnboardingResponse { Success = false, Message = "Submit failed." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit onboarding.");
            return new SubmitOnboardingResponse { Success = false, Message = "Submit failed." };
        }
    }
}
