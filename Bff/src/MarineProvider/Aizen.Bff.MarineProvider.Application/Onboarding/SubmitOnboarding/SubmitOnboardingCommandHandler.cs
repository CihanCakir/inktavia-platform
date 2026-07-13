using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Onboarding.SubmitOnboarding;

public sealed class SubmitOnboardingCommandHandler
    : AizenCommandHandler<SubmitOnboardingCommand, SubmitOnboardingResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<SubmitOnboardingCommandHandler> _logger;

    public SubmitOnboardingCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityRemoteCall identity,
        ILogger<SubmitOnboardingCommandHandler> logger)
    {
        _resolver = resolver;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<SubmitOnboardingResponse?> Handle(SubmitOnboardingCommand request, CancellationToken ct)
    {
        var resolution = await _resolver.ResolveAsync(ct);
        var profileId = resolution.ProfileId ?? 0;
        if (profileId <= 0)
            return new SubmitOnboardingResponse { Success = false, Message = "Provider profile not found." };

        try
        {
            var result = await _identity.SubmitProviderOnboarding(profileId);
            var data = result.Body;
            if (data is null)
                return new SubmitOnboardingResponse { Success = false, Message = "Submit failed." };

            return data.Success
                ? new SubmitOnboardingResponse { Success = true, Message = data.Message }
                : new SubmitOnboardingResponse { Success = false, Message = data.Message };
        }
        catch (Refit.ApiException ex)
        {
            var message = ExtractBusinessMessage(ex.Content) ?? "An error occurred while submitting onboarding.";
            _logger.LogWarning(ex, "Submit onboarding rejected for profile {ProfileId}: {Message}", profileId, message);
            return new SubmitOnboardingResponse { Success = false, Message = message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit onboarding.");
            return new SubmitOnboardingResponse { Success = false, Message = "An error occurred while submitting onboarding." };
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
