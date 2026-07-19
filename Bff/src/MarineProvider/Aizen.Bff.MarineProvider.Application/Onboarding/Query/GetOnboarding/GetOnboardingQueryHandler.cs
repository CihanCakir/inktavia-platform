using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Aizen.Bff.MarineProvider.Application.Onboarding;

public sealed class GetOnboardingQueryHandler
    : AizenQueryHandler<GetOnboardingQuery, OnboardingResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<GetOnboardingQueryHandler> _logger;

    public GetOnboardingQueryHandler(
        IProviderProfileResolver resolver,
        IIdentityRemoteCall identity,
        ILogger<GetOnboardingQueryHandler> logger)
    {
        _resolver = resolver;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<OnboardingResponse?> Handle(GetOnboardingQuery request, CancellationToken ct)
    {
        var resolution = await _resolver.ResolveAsync(ct);
        var profileId = resolution.ProfileId ?? 0;
        if (profileId <= 0)
            return new OnboardingResponse { Status = "Error", Draft = null };

        try
        {
            var result = await _identity.GetProviderOnboarding(profileId);
            var body = result.Body;
            if (body is null)
                return new OnboardingResponse { ProfileId = profileId, Status = "NotFound" };

            return new OnboardingResponse
            {
                ProfileId = body.ProfileId,
                Status = body.Status,
                SchemaVersion = body.SchemaVersion,
                StepStatuses = body.StepStatuses,
                Draft = ParseDraft(body.DraftJson),
                RevisionSteps = body.RevisionSteps,
                RevisionNote = body.RevisionNote,
                LastSavedAtUtc = body.LastSavedAtUtc,
                SubmittedAtUtc = body.SubmittedAtUtc,
                Documents = body.Documents,
            };
        }
        catch (Refit.ApiException ex)
        {
            var message = ExtractBusinessMessage(ex.Content) ?? "Failed to get onboarding.";
            _logger.LogWarning(ex, "Get onboarding rejected for profile {ProfileId}: {Message}", profileId, message);
            return new OnboardingResponse { ProfileId = profileId, Status = "Error" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get onboarding for profile {ProfileId}.", profileId);
            return new OnboardingResponse { ProfileId = profileId, Status = "Error" };
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

    /// <summary>Identity sends the draft as raw JSON; the SPA wants a real object. A malformed draft is a
    /// server-side bug, not something to hide behind an empty form — log it and return null.</summary>
    private static JToken? ParseDraft(string? draftJson)
    {
        if (string.IsNullOrWhiteSpace(draftJson)) return null;
        try { return JToken.Parse(draftJson); }
        catch (Newtonsoft.Json.JsonReaderException) { return null; }
    }
}