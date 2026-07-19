using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Onboarding;

public sealed class AttachOnboardingDocumentCommandHandler
    : AizenCommandHandler<AttachOnboardingDocumentCommand, AttachDocumentBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<AttachOnboardingDocumentCommandHandler> _logger;

    public AttachOnboardingDocumentCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IIdentityRemoteCall identity,
        ILogger<AttachOnboardingDocumentCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<AttachDocumentBffResponse?> Handle(AttachOnboardingDocumentCommand request, CancellationToken ct)
    {
        // Resolve identity first → populates IProviderIdentityHolder → assertion headers on the module call.
        // Without this the holder stays empty, the module call carries no asserted user, and Identity sees
        // callerUserId = 0 → "You do not have permission to modify this profile."
        var resolution = await _resolver.ResolveAsync(ct);
        var profileId = resolution.ProfileId ?? 0;
        if (profileId <= 0)
            return new AttachDocumentBffResponse { Success = false, Message = "Provider profile not found." };

        // Fail closed: a missing user id is a bug, not a caller we can silently treat as user 0.
        var userId = _identityHolder.UserId ?? 0;
        if (userId <= 0)
        {
            _logger.LogError("Provider identity unresolved for profile {ProfileId}; refusing to attach document.", profileId);
            return new AttachDocumentBffResponse { Success = false, Message = "Provider identity could not be resolved." };
        }

        try
        {
            var result = await _identity.AttachProviderDocument(profileId, new AttachProviderDocumentRequest
            {
                FileId = request.FileId,
                DocumentType = request.DocumentType,
                Issuer = request.Issuer,
                UserId = userId,
            });

            var data = result.Body;
            if (data is null)
                return new AttachDocumentBffResponse { Success = false, Message = "Failed to attach document." };

            return new AttachDocumentBffResponse
            {
                Success = data.Success,
                Message = data.Success ? "Document attached successfully." : "Failed to attach document.",
                Document = data.Document,
            };
        }
        catch (Refit.ApiException ex)
        {
            // Identity maps AizenBusinessException → HTTP 400 with an Aizen envelope. Refit throws on non-2xx, so
            // without this the rule that was actually violated ("You do not own this file", "already attached",
            // "not ready for attachment", …) is replaced by a generic message and lost to the caller.
            var message = ExtractBusinessMessage(ex.Content) ?? "An error occurred while attaching the document.";
            _logger.LogWarning(ex, "Attach rejected for document {FileId}, profile {ProfileId}: {Message}",
                request.FileId, profileId, message);
            return new AttachDocumentBffResponse { Success = false, Message = message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach document {FileId} for profile {ProfileId}.", request.FileId, profileId);
            return new AttachDocumentBffResponse { Success = false, Message = "An error occurred while attaching the document." };
        }
    }

    /// <summary>Pulls <c>header.errorMessage</c> out of an Aizen error envelope, if the body is one.</summary>
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
        catch (System.Text.Json.JsonException)
        {
            // Not an Aizen envelope — fall through to the generic message.
        }
        return null;
    }
}
