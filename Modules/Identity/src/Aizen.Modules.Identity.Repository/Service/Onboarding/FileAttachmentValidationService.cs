using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Common.Abstraction.ViewModel; // FAZ12B #65: AizenErrorCode
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;
using Aizen.Modules.Identity.Abstraction.Options;
using Aizen.Modules.Identity.Abstraction.RemoteCall;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Enum;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.Onboarding;

/// <summary>
/// Shared validate + persist + claim pipeline for attaching a verified file to an organizer profile.
/// Both the BFF handler (<c>AttachProviderDocumentCommandHandler</c>) and the legacy admin handler
/// (<c>AddOrganizerVerificationDocumentCommandHandler</c>) delegate here instead of duplicating logic.
///
/// Validation pipeline (fail-closed):
///   1. Profile exists + is Organizer + not deleted
///   2. Caller owns the profile (profile.UserId == callerUserId) — skipped when <c>actorIsAdmin</c> is true
///   3. Onboarding not Submitted (documents immutable post-submit)
///   4. Duplicate FilePublicId guard (same profile)
///   5. Cross-profile FilePublicId uniqueness
///   6. FileStorage: file exists (fail-closed)
///   7. FileStorage: file was uploaded by the profile owner (profile.UserId) — always checked
///   8. FileStorage: status is Uploaded or Ready
///   9. Content-type allowlist (pdf, jpeg, png)
///  10. Size ≤ 10 MB
///  11. Claim file via LinkToOwner with the document's real PublicId (fail-closed)
///  12. Persist with snapshots from FileStorage (never from client)
/// </summary>
public interface IFileAttachmentValidationService
{
    /// <summary>
    /// Validates the file, persists the document, and claims it in FileStorage.
    /// </summary>
    /// <param name="profileId">Target organizer profile.</param>
    /// <param name="callerUserId">The user performing the action.</param>
    /// <param name="filePublicId">Public identifier of the file in FileStorage.</param>
    /// <param name="documentType">Document type label (e.g. "trade_license").</param>
    /// <param name="issuer">Optional issuing authority.</param>
    /// <param name="actorIsAdmin">
    /// When true, skips profile-ownership check (admin doesn't own the profile) but still
    /// verifies the file was uploaded by the profile owner (prevents cross-user file attachment).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="VerificationDocumentEntity"/>.</returns>
    Task<VerificationDocumentEntity> ValidateAndAttachAsync(
        long profileId,
        long callerUserId,
        Guid filePublicId,
        string documentType,
        string? issuer,
        bool actorIsAdmin,
        CancellationToken ct,
        WorkshopRoleContext roleContext = WorkshopRoleContext.Organizer);
}

public sealed class FileAttachmentValidationService : IFileAttachmentValidationService
{
    private readonly IdentityDbContext _db;
    private readonly IIdentityFileStorageRemoteCall _fileStorage;
    private readonly IdentityKeycloakOptions _kcOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FileAttachmentValidationService> _logger;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png"
    };
    private const long MaxSizeBytes = 10 * 1024 * 1024;

    public FileAttachmentValidationService(
        IdentityDbContext db,
        IIdentityFileStorageRemoteCall fileStorage,
        IOptions<IdentityKeycloakOptions> kcOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<FileAttachmentValidationService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _kcOptions = kcOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<VerificationDocumentEntity> ValidateAndAttachAsync(
        long profileId,
        long callerUserId,
        Guid filePublicId,
        string documentType,
        string? issuer,
        bool actorIsAdmin,
        CancellationToken ct,
        WorkshopRoleContext roleContext = WorkshopRoleContext.Organizer)
    {
        // 0. Reject invalid caller identity for non-admin paths
        if (!actorIsAdmin && callerUserId <= 0)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingCallerIdentityInvalid, "Invalid caller identity.");

        // 1. Profile exists + matches expected role
        var profile = await _db.UserProfiles
            .Include(p => p.VerificationDocuments)
            .FirstOrDefaultAsync(p => p.Id == profileId
                && p.RoleContext == roleContext
                && !p.IsDeleted, ct)
            ?? throw new AizenBusinessException((int)AizenErrorCode.ProviderProfileNotFound, "Profile not found.");

        // 2. Caller owns the profile (skip for admin path — admin doesn't own the profile)
        if (!actorIsAdmin && profile.UserId != callerUserId)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingPermissionDenied, "You do not have permission to modify this profile.");

        // 3. Onboarding not Submitted
        var onboarding = await _db.ProviderOnboarding
            .FirstOrDefaultAsync(o => o.ProfileId == profileId, ct);
        if (onboarding is not null && onboarding.Status == ProviderOnboardingStatus.Submitted)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingLockedAfterSubmit, "Cannot attach documents after onboarding has been submitted.");

        // 4. Duplicate on this profile
        if (profile.VerificationDocuments.Any(d => d.FilePublicId == filePublicId && !d.IsDeleted))
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingFileAlreadyAttached, "This file is already attached to the profile.");

        // 5. Cross-profile uniqueness
        if (await _db.VerificationDocuments.AnyAsync(
            d => d.FilePublicId == filePublicId && d.ProfileId != profileId && !d.IsDeleted, ct))
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingFileAssociatedWithOtherProfile, "This file is already associated with another profile.");

        // 6–10. FileStorage validation — FAIL CLOSED. Any error → reject.
        var token = await GetServiceTokenAsync(ct);
        var bearerToken = $"Bearer {token}";

        FileStorage.Abstraction.Dto.File.FileDto file;
        try
        {
            var fileResult = await _fileStorage.GetFile(filePublicId, bearerToken);
            file = fileResult?.Body
                ?? throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingFileNotFoundInStorage, "File not found in storage.");
        }
        catch (AizenBusinessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FileStorage validation failed for file {FileId}. Rejecting attach.", filePublicId);
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingFileVerificationFailed, "Unable to verify the file. Please try again.");
        }

        // 7. Ownership — the file must have been uploaded by the profile owner.
        //    For non-admin: profile.UserId == callerUserId (checked in step 2), so this is equivalent.
        //    For admin: we skip the profile-ownership check but still verify the file belongs to the profile owner.
        if (file.UploadedByUserId != profile.UserId)
        {
            _logger.LogWarning("Ownership violation: file {FileId} uploaded by {UploadedBy}, but profile {ProfileId} is owned by {ProfileOwner}.",
                filePublicId, file.UploadedByUserId, profileId, profile.UserId);
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingFileNotOwnedByProfile, "The file was not uploaded by the profile owner.");
        }

        // 8. Status — allow Uploaded (scan pending) and Ready (scan passed).
        //    Quarantined files (threat detected by AV scan) are explicitly rejected.
        if (file.Status == FileStatus.Quarantined)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingFileQuarantined, "File has been quarantined due to a detected threat and cannot be attached.");
        if (file.Status != FileStatus.Uploaded && file.Status != FileStatus.Ready)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingFileNotReady, $"File is not ready for attachment (status: {file.Status}).");

        // 9. Content-type
        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingFileTypeNotAllowed, $"File type '{file.ContentType}' is not allowed. Accepted: PDF, JPEG, PNG.");

        // 10. Size
        if (file.SizeInBytes > MaxSizeBytes)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingFileTooLarge, $"File is too large ({file.SizeInBytes / (1024 * 1024)} MB). Maximum: 10 MB.");

        // 11. Create document entity (not yet persisted)
        var document = VerificationDocumentEntity.CreateFromBff(
            profileId: profile.Id,
            filePublicId: filePublicId,
            documentType: documentType,
            issuer: issuer,
            name: file.OriginalFileName,
            contentType: file.ContentType,
            sizeInBytes: file.SizeInBytes,
            uploadedByUserId: file.UploadedByUserId ?? 0);

        var documentPublicId = document.PublicId
            ?? throw new InvalidOperationException("Document has no PublicId before persistence.");

        // 12. Persist the document FIRST — if this fails no claim is created, so no
        //     phantom ownership reference pointing at a non-existent document.
        profile.AddVerificationDocument(document);
        await _db.SaveChangesAsync(ct);

        // 13. Claim the file SECOND. If the claim fails, roll back the document row
        //     so we don't leave a dangling document. The file remains unclaimed and the
        //     orphan cleanup sweep will reap it normally.
        try
        {
            await _fileStorage.LinkToOwner(filePublicId, new LinkFileToOwnerRequest
            {
                OwnerModule = "Identity",
                OwnerEntityType = "OrganizerVerificationDocument",
                OwnerEntityId = documentPublicId,
            }, bearerToken);

            _logger.LogInformation("File {FileId} claimed for document {DocumentPublicId} on profile {ProfileId}.",
                filePublicId, documentPublicId, profileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to claim file {FileId} for document {DocumentPublicId}. Rolling back document row.",
                filePublicId, documentPublicId);

            // Roll back: remove the document row so we don't leave a dangling reference
            try
            {
                _db.VerificationDocuments.Remove(document);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx,
                    "Failed to roll back document {DocumentPublicId} after claim failure. Document row may be orphaned.",
                    documentPublicId);
            }

            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingFileVerificationFailed, "Unable to complete file attachment. Please try again.");
        }

        return document;
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
