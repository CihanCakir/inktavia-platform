using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Common.Abstraction.ViewModel; // FAZ12B #65: AizenErrorCode
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Abstraction.Options;
using Aizen.Modules.Identity.Abstraction.RemoteCall;
using Aizen.Modules.Identity.Domain.Enum;
using Aizen.Modules.Identity.Repository.Context;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.RemoveProviderDocument;

/// <summary>
/// Removes (soft-deletes) a verification document from an organizer profile.
/// Validates profile ownership and that onboarding is not submitted.
/// After removing the document row, triggers a soft-delete on the file in FileStorage.
/// </summary>
public sealed class RemoveProviderDocumentCommandHandler
    : AizenCommandHandler<RemoveProviderDocumentCommand, RemoveProviderDocumentResponse>
{
    private readonly IdentityDbContext _db;
    private readonly IIdentityFileStorageRemoteCall _fileStorage;
    private readonly IdentityKeycloakOptions _kcOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RemoveProviderDocumentCommandHandler> _logger;

    public RemoveProviderDocumentCommandHandler(
        IdentityDbContext db,
        IIdentityFileStorageRemoteCall fileStorage,
        IOptions<IdentityKeycloakOptions> kcOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<RemoveProviderDocumentCommandHandler> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _kcOptions = kcOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public override async Task<RemoveProviderDocumentResponse?> Handle(
        RemoveProviderDocumentCommand request, CancellationToken ct)
    {
        // 1. Load profile with documents
        var profile = await _db.UserProfiles
            .Include(p => p.VerificationDocuments)
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId
                && p.RoleContext == WorkshopRoleContext.Organizer
                && !p.IsDeleted, ct);

        if (profile is null)
        {
            _logger.LogWarning("RemoveProviderDocument: profile {ProfileId} not found.", request.ProfileId);
            throw new AizenBusinessException((int)AizenErrorCode.ProviderProfileNotFound, "Profile not found.");
        }

        // 2. Guard: cannot remove after onboarding is submitted
        var onboarding = await _db.ProviderOnboarding
            .FirstOrDefaultAsync(o => o.ProfileId == request.ProfileId, ct);

        if (onboarding is not null && onboarding.Status == ProviderOnboardingStatus.Submitted)
        {
            _logger.LogWarning("RemoveProviderDocument: onboarding already submitted for profile {ProfileId}.", request.ProfileId);
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingLockedAfterSubmit, "Cannot remove documents after onboarding has been submitted.");
        }

        // 3. Remove (soft-delete the document row)
        var removed = profile.RemoveVerificationDocument(request.FileId);
        if (!removed)
        {
            _logger.LogWarning("RemoveProviderDocument: file {FileId} not found on profile {ProfileId}.", request.FileId, request.ProfileId);
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingDocumentNotFound, "Document not found on this profile.");
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("RemoveProviderDocument: removed file {FileId} from profile {ProfileId}.",
            request.FileId, request.ProfileId);

        // 4. Trigger soft-delete on the file in FileStorage (best-effort; document row is already removed).
        //    If the file has other owners, FileStorage will keep it; otherwise it will be soft-deleted
        //    and eventually cleaned up physically.
        try
        {
            var token = await GetServiceTokenAsync(ct);
            var bearerToken = $"Bearer {token}";
            await _fileStorage.DeleteFile(request.FileId, new DeleteFileRequest(), bearerToken);
            _logger.LogInformation("RemoveProviderDocument: triggered file soft-delete for {FileId}.", request.FileId);
        }
        catch (Exception ex)
        {
            // Best-effort: the document row is already removed. Log and continue.
            _logger.LogWarning(ex, "RemoveProviderDocument: failed to soft-delete file {FileId} in FileStorage. Orphaned file may remain.", request.FileId);
        }

        return new RemoveProviderDocumentResponse { Success = true };
    }

    private async Task<string> GetServiceTokenAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _kcOptions.AdminClientId,
            ["client_secret"] = _kcOptions.AdminClientSecret,
        });
        var url = $"{_kcOptions.BaseUrl}/realms/{_kcOptions.Realm}/protocol/openid-connect/token";
        var response = await client.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("No access_token in Keycloak token response.");
    }
}
