using Aizen.Bff.MarineProvider.Application.Common;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;
using Aizen.Core.Common.Abstraction.ViewModel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Onboarding;

public sealed class SubmitOnboardingCommandHandler
    : AizenCommandHandler<SubmitOnboardingCommand, SubmitOnboardingResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<SubmitOnboardingCommandHandler> _logger;

    public SubmitOnboardingCommandHandler(
        IProviderProfileResolver resolver,
        IIdentityRemoteCall identity,
        ILogger<SubmitOnboardingCommandHandler> logger)
    {
        _resolver = resolver;
        _identity = identity;
        _logger = logger;
    }

    // FAZ13A #67: başarısızlık 200'de gizlenmiyor — AizenBusinessException olarak fırlatılıyor (bkz. SaveOnboardingStep).
    public override async Task<SubmitOnboardingResponse?> Handle(SubmitOnboardingCommand request, CancellationToken ct)
    {
        var resolution = await _resolver.ResolveAsync(ct);
        var profileId = resolution.ProfileId ?? 0;
        if (profileId <= 0)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderProfileNotFound, "Provider profile not found.");

        SubmitProviderOnboardingResponse? data;
        try
        {
            var result = await _identity.SubmitProviderOnboarding(profileId);
            data = result.Body;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Submit onboarding rejected for profile {ProfileId}.", profileId);
            throw ModuleFailurePropagation.FromApiException(ex, "An error occurred while submitting onboarding.");
        }

        if (data is null || !data.Success)
            throw new AizenBusinessException(data?.Message ?? "Submit failed.");

        return new SubmitOnboardingResponse { Success = true, Message = data.Message };
    }
}
