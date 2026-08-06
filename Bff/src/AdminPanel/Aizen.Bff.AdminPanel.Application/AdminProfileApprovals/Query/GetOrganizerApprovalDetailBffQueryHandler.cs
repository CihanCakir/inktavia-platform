using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Query;

[DocumentationInfo("Get organizer approval detail BFF query handler",
    "Fetches organizer profile detail, with-user info, and provider onboarding state in parallel from Identity. " +
    "Documents from both verification upload and onboarding wizard are enriched with signed read URLs from FileStorage.")]
public sealed class GetOrganizerApprovalDetailBffQueryHandler
    : AizenQueryHandler<GetOrganizerApprovalDetailBffQuery, OrganizerApprovalDetailBffResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetOrganizerApprovalDetailBffQueryHandler> _logger;

    public GetOrganizerApprovalDetailBffQueryHandler(
        IIdentityRemoteCall identity,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetOrganizerApprovalDetailBffQueryHandler> logger)
    {
        _identity = identity;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<OrganizerApprovalDetailBffResponse?> Handle(
        GetOrganizerApprovalDetailBffQuery request, CancellationToken cancellationToken)
    {
        var response = new OrganizerApprovalDetailBffResponse();

        try
        {
            // Fetch profile detail, user contact info, and onboarding state in parallel.
            var detailTask      = _identity.GetAdminOrganizerProfileOnly(request.ProfileId);
            var withUserTask    = _identity.GetAdminOrganizerProfileWithUser(request.ProfileId);
            var onboardingTask  = _identity.GetProviderOnboardingAdmin(request.ProfileId);

            await Task.WhenAll(detailTask, withUserTask, onboardingTask);

            var detailResult     = await detailTask;
            var withUserResult   = await withUserTask;
            var onboardingResult = await onboardingTask;

            if (detailResult?.Header?.IsSuccess != true || detailResult.Body == null)
            {
                _logger.LogWarning("[OrganizerApprovalDetailBff] Profile {ProfileId} not found.", request.ProfileId);
                return response;
            }

            var detail = detailResult.Body;

            OrganizerProfileWithUserDetailDto? withUser = null;
            if (withUserResult?.Header?.IsSuccess == true)
                withUser = withUserResult.Body;
            else
                response.Warnings.Add(AdminBffWarning.CallFailed("Identity.OrganizerWithUser", "Could not fetch user contact info."));

            ProviderOnboardingResponse? onboarding = null;
            if (onboardingResult?.Header?.IsSuccess == true)
                onboarding = onboardingResult.Body;
            else
                _logger.LogDebug("[OrganizerApprovalDetailBff] No onboarding record for profile {ProfileId} (may not be a Marine Provider).", request.ProfileId);

            // ── Collect all FileIds that need signed URLs ──────────────────────
            // 1. Verification documents (uploaded by admin on behalf of provider)
            var verificationDocs = detail.Documents ?? new List<VerificationDocumentDto>();
            // 2. Onboarding documents (uploaded by provider during wizard)
            var onboardingDocs = onboarding?.Documents ?? new List<ProviderDocumentDto>();

            var allFileIds = verificationDocs
                .Select(d => d.FileId)
                .Concat(onboardingDocs.Select(d => d.FileId))
                .Distinct()
                .ToList();

            // ── Enrich with signed read URLs (best-effort) ─────────────────────
            var signedUrlMap = new Dictionary<Guid, string?>();
            if (allFileIds.Count > 0)
            {
                try
                {
                    var urlRequest = new CreateFileReadUrlRemoteCallRequest { ExpiresIn = TimeSpan.FromMinutes(15) };
                    var urlTasks   = allFileIds.Select(id => _fileStorage.CreateReadUrl(id, urlRequest)).ToList();

                    await Task.WhenAll(urlTasks.Select(t => t.ContinueWith(_ => { }, TaskScheduler.Default)));

                    for (var i = 0; i < allFileIds.Count; i++)
                    {
                        var task = urlTasks[i];
                        signedUrlMap[allFileIds[i]] =
                            task.IsCompletedSuccessfully && task.Result?.Header?.IsSuccess == true
                                ? task.Result.Body?.AccessUrl?.ReadUrl
                                : null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[OrganizerApprovalDetailBff] FileStorage signed URL fetch failed for profile {ProfileId}.", request.ProfileId);
                    response.Warnings.Add(AdminBffWarning.CallFailed("FileStorage", "Could not fetch signed document URLs."));
                }
            }

            response.Organizer = MapToDetailDto(detail, withUser, onboarding, signedUrlMap);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OrganizerApprovalDetailBff] Identity call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }

    private static OrganizerApprovalDetailBffDto MapToDetailDto(
        OrganizerProfileDetailDto        detail,
        OrganizerProfileWithUserDetailDto? withUser,
        ProviderOnboardingResponse?      onboarding,
        Dictionary<Guid, string?>        signedUrlMap)
    {
        var reviewedAt = detail.ApprovalStatus?.ToLowerInvariant() switch
        {
            "approved" => detail.ApprovedAt?.ToString("O"),
            "rejected" => detail.RejectedAt?.ToString("O"),
            _ => null
        };

        var fullName = $"{detail.FirstName} {detail.LastName}".Trim();

        return new OrganizerApprovalDetailBffDto
        {
            UserId          = detail.UserId,
            ProfileId       = detail.Id,
            Status          = MapApprovalStatus(detail.ApprovalStatus),
            ReviewedBy      = null,
            ReviewedAt      = reviewedAt,
            RejectionCategory = null,
            RejectionReason = detail.RejectReason,
            InternalNote    = null,

            Applicant = new OrganizerApplicantBffDto
            {
                FullName      = string.IsNullOrEmpty(fullName) ? null : fullName,
                Email         = withUser?.Email,
                Phone         = withUser?.PhoneNumber,
                AvatarUrl     = detail.ProfilePhotoUrl,
                RegisteredAt  = withUser?.UserCreatedAt?.ToString("O"),
                IdentityType  = withUser?.LoginType,
                Role          = "Organizer"
            },

            Company = new OrganizerCompanyBffDto(),

            Checklist = new ProfileApprovalChecklistBffDto
            {
                EmailVerified         = false,
                PhoneVerified         = false,
                CompanyNameProvided   = false,
                TaxNumberProvided     = false,
                DocumentsUploaded     = (detail.Documents?.Count > 0) || (onboarding?.Documents?.Count > 0),
                DuplicateAccountFound = false,
                SuspiciousActivityFound = false
            },

            // Verification documents (uploaded by admin / uploaded via BFF document upload flow)
            Documents = detail.Documents?.Select(d => new ProfileApprovalDocumentBffDto
            {
                Id         = d.Id.ToString(),
                Type       = d.DocumentType,
                Name       = d.Name,
                FileId     = d.FileId.ToString(),
                Url        = signedUrlMap.GetValueOrDefault(d.FileId),
                Format     = d.Format,
                Size       = d.FileSizeDisplay,
                Issuer     = d.Issuer,
                MatchScore = d.MatchScore,
                UploadedAt = d.UploadedAt ?? string.Empty
            }).ToList() ?? new List<ProfileApprovalDocumentBffDto>(),

            RiskSignals = detail.RiskSignals?.Select(r => new ProfileApprovalRiskSignalBffDto
            {
                Level       = r.Severity,
                Title       = r.Title,
                Description = r.Description
            }).ToList() ?? new List<ProfileApprovalRiskSignalBffDto>(),

            Activity = new List<ProfileApprovalActivityItemBffDto>(),

            // Provider onboarding wizard state (null for non-Marine-Provider organizers)
            Onboarding = onboarding is null ? null : MapOnboarding(onboarding, signedUrlMap),

            Warnings = new List<AdminBffWarning>()
        };
    }

    private static ProviderOnboardingBffDto MapOnboarding(
        ProviderOnboardingResponse onboarding,
        Dictionary<Guid, string?> signedUrlMap)
    {
        return new ProviderOnboardingBffDto
        {
            Status          = onboarding.Status,
            StepStatuses    = onboarding.StepStatuses,
            RevisionNote    = onboarding.RevisionNote,
            RevisionSteps   = onboarding.RevisionSteps,
            SubmittedAtUtc  = onboarding.SubmittedAtUtc?.ToString("O"),
            Documents       = onboarding.Documents?.Select(d => new ProviderOnboardingDocumentBffDto
            {
                FileId         = d.FileId.ToString(),
                FileName       = d.FileName,
                DocumentType   = d.DocumentType,
                ContentType    = d.ContentType,
                SizeInBytes    = d.SizeInBytes,
                Issuer         = d.Issuer,
                UploadedAt     = d.UploadedAt.ToString("O"),
                ReviewStatus   = d.ReviewStatus,
                ResolutionNote = d.ResolutionNote,
                Url            = signedUrlMap.GetValueOrDefault(d.FileId)
            }).ToList() ?? new List<ProviderOnboardingDocumentBffDto>()
        };
    }

    private static string MapApprovalStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "pending"  => "pending",
        "approved" => "approved",
        "rejected" => "rejected",
        _          => "pending"
    };
}
