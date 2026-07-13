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
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<SaveOnboardingStepCommandHandler> _logger;

    public SaveOnboardingStepCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityRemoteCall identity,
        ILogger<SaveOnboardingStepCommandHandler> logger)
    {
        _resolver = resolver;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<SaveOnboardingStepResponse?> Handle(SaveOnboardingStepCommand request, CancellationToken ct)
    {
        var resolution = await _resolver.ResolveAsync(ct);
        var profileId = resolution.ProfileId ?? 0;
        if (profileId <= 0)
            return new SaveOnboardingStepResponse { Success = false, Message = "Provider profile not found." };

        try
        {
            var result = await _identity.SaveProviderOnboardingStep(profileId, request.Step,
                new SaveProviderOnboardingStepRequest
                {
                    StepStatus = request.StepStatus,
                    StepDataJson = request.StepDataJson,
                    SchemaVersion = request.SchemaVersion,
                });
            var data = result.Body;
            if (data is null)
                return new SaveOnboardingStepResponse { Success = false, Message = "Save failed." };

            return data.Success
                ? new SaveOnboardingStepResponse { Success = true, Message = data.Message }
                : new SaveOnboardingStepResponse { Success = false, Message = data.Message };
        }
        catch (Refit.ApiException ex)
        {
            var message = ExtractBusinessMessage(ex.Content) ?? "An error occurred while saving the step.";
            _logger.LogWarning(ex, "Save step rejected for step {Step}, profile {ProfileId}: {Message}",
                request.Step, profileId, message);
            return new SaveOnboardingStepResponse { Success = false, Message = message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save onboarding step {Step}.", request.Step);
            return new SaveOnboardingStepResponse { Success = false, Message = "An error occurred while saving the step." };
        }
    }

    private static string? ExtractBusinessMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("header", out var header)
                && header.TryGetProperty("errorMessage", out var msg)
                && msg.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = msg.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (System.Text.Json.JsonException) { }
        return null;
    }
}
