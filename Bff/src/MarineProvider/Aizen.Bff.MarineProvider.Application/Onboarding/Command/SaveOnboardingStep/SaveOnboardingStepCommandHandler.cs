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

public sealed class SaveOnboardingStepCommandHandler
    : AizenCommandHandler<SaveOnboardingStepCommand, SaveOnboardingStepResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<SaveOnboardingStepCommandHandler> _logger;

    public SaveOnboardingStepCommandHandler(
        IProviderProfileResolver resolver,
        IIdentityRemoteCall identity,
        ILogger<SaveOnboardingStepCommandHandler> logger)
    {
        _resolver = resolver;
        _identity = identity;
        _logger = logger;
    }

    // FAZ13A #67: başarısızlık artık 200 gövdesinde { Success=false } olarak GİZLENMİYOR — AizenBusinessException
    // olarak fırlatılıyor → BFF middleware düzgün bir 400 failure envelope üretir (kod + yerelleştirilmiş mesaj),
    // frontend'in resolveApiErrorMessage'ı kodu eşleyebilir. (CargoDryInterest de bu handler'dan geçer — saveStep.)
    public override async Task<SaveOnboardingStepResponse?> Handle(SaveOnboardingStepCommand request, CancellationToken ct)
    {
        var resolution = await _resolver.ResolveAsync(ct);
        var profileId = resolution.ProfileId ?? 0;
        if (profileId <= 0)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderProfileNotFound, "Provider profile not found.");

        SaveProviderOnboardingStepResponse? data;
        try
        {
            var result = await _identity.SaveProviderOnboardingStep(profileId, request.Step,
                new SaveProviderOnboardingStepRequest
                {
                    StepStatus = request.StepStatus,
                    StepDataJson = request.StepDataJson,
                    SchemaVersion = request.SchemaVersion,
                });
            data = result.Body;
        }
        catch (Refit.ApiException ex)
        {
            // Modülün kararlı hata zarfını (code + Task A ile yerelleştirilmiş mesaj) aynen yukarı taşı.
            _logger.LogWarning(ex, "Save step rejected for step {Step}, profile {ProfileId}.", request.Step, profileId);
            throw ModuleFailurePropagation.FromApiException(ex, "An error occurred while saving the step.");
        }

        // Modül 200 + { Success:false } dönerse (Faz 12B sonrası artık fırlattığı için nadir) yine de yüzeye çıkar.
        if (data is null || !data.Success)
            throw new AizenBusinessException(data?.Message ?? "Save failed.");

        return new SaveOnboardingStepResponse { Success = true, Message = data.Message };
    }
}
