using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Onboarding.SaveOnboardingStep;

public sealed class SaveOnboardingStepCommandHandler
    : AizenCommandHandler<SaveOnboardingStepCommand, SaveOnboardingStepResponse>
{
    private readonly IProviderContext _context;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<SaveOnboardingStepCommandHandler> _logger;

    public SaveOnboardingStepCommandHandler(IProviderContext context, IProviderIdentityRemoteCall identity, ILogger<SaveOnboardingStepCommandHandler> logger)
    { _context = context; _identity = identity; _logger = logger; }

    public override async Task<SaveOnboardingStepResponse?> Handle(SaveOnboardingStepCommand request, CancellationToken ct)
    {
        var profileId = _context.ProviderProfileId ?? 0;
        if (profileId <= 0) return new SaveOnboardingStepResponse { Success = false, Message = "Provider profile not found." };

        try
        {
            var result = await _identity.SaveProviderOnboardingStep(profileId, request.Step,
                new SaveProviderOnboardingStepRequest
                {
                    StepStatus = request.StepStatus,
                    StepData = request.StepData,
                    SchemaVersion = request.SchemaVersion,
                });
            var data = result.Body;
            return data is not null
                ? new SaveOnboardingStepResponse { Success = data.Success, Message = data.Message }
                : new SaveOnboardingStepResponse { Success = false, Message = "Save failed." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save onboarding step {Step}.", request.Step);
            return new SaveOnboardingStepResponse { Success = false, Message = "Save failed." };
        }
    }
}
