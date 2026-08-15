using Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.ProfileApprovals.Query;

[DocumentationInfo("Get venue approval detail BFF query handler", "Fetches venue profile detail and with-user info in parallel from Identity, maps to BFF review DTO.")]
public sealed class GetVenueApprovalDetailBffQueryHandler
    : AizenQueryHandler<GetVenueApprovalDetailBffQuery, VenueApprovalDetailBffResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetVenueApprovalDetailBffQueryHandler> _logger;

    public GetVenueApprovalDetailBffQueryHandler(
        IIdentityRemoteCall identity,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetVenueApprovalDetailBffQueryHandler> logger)
    {
        _identity = identity;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<VenueApprovalDetailBffResponse?> Handle(
        GetVenueApprovalDetailBffQuery request, CancellationToken cancellationToken)
    {
        var response = new VenueApprovalDetailBffResponse();

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VenueApprovalDetailBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        try
        {
            // Fetch detail-only (for RejectReason) and with-user (for Email/Phone) in parallel.
            var detailTask = _identity.GetAdminVenueProfileOnly(
                request.ProfileId);
            var withUserTask = _identity.GetAdminVenueProfileWithUser(
                request.ProfileId);

            await Task.WhenAll(detailTask, withUserTask);

            var detailResult = await detailTask;
            var withUserResult = await withUserTask;

            if (detailResult?.Header?.IsSuccess != true || detailResult.Body == null)
            {
                _logger.LogWarning("[VenueApprovalDetailBff] Profile {ProfileId} not found.", request.ProfileId);
                return response;
            }

            var detail = detailResult.Body;
            VenueProfileWithUserDetailDto? withUser = null;

            if (withUserResult?.Header?.IsSuccess == true)
                withUser = withUserResult.Body;
            else
                response.Warnings.Add(AdminBffWarning.CallFailed("Identity.VenueWithUser", "Could not fetch user contact info."));

            // Enrich documents with signed read URLs from FileStorage (best-effort: failure yields url=null).
            var signedUrlMap = new Dictionary<Guid, string?>();
            if (detail.Documents?.Count > 0)
            {
                try
                {
                    var urlRequest = new CreateFileReadUrlRemoteCallRequest { ExpiresIn = TimeSpan.FromMinutes(15) };
                    var urlTasks = detail.Documents
                        .Select(d => _fileStorage.CreateReadUrl(d.FileId, urlRequest))
                        .ToList();

                    await Task.WhenAll(urlTasks.Select(t => t.ContinueWith(_ => { }, TaskScheduler.Default)));

                    for (var i = 0; i < detail.Documents.Count; i++)
                    {
                        var task = urlTasks[i];
                        signedUrlMap[detail.Documents[i].FileId] =
                            task.IsCompletedSuccessfully && task.Result?.Header?.IsSuccess == true
                                ? task.Result.Body?.ReadUrl
                                : null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[VenueApprovalDetailBff] FileStorage signed URL fetch failed for profile {ProfileId}.", request.ProfileId);
                    response.Warnings.Add(AdminBffWarning.CallFailed("FileStorage", "Could not fetch signed document URLs."));
                }
            }

            response.Venue = MapToDetailDto(detail, withUser, signedUrlMap);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VenueApprovalDetailBff] Identity call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }

    private static VenueApprovalDetailBffDto MapToDetailDto(
        VenueProfileDetailDto detail,
        VenueProfileWithUserDetailDto? withUser,
        Dictionary<Guid, string?> signedUrlMap)
    {
        var reviewedAt = detail.ApprovalStatus?.ToLowerInvariant() switch
        {
            "approved" => detail.ApprovedAt?.ToString("O"),
            "rejected" => detail.RejectedAt?.ToString("O"),
            _ => null
        };

        var fullName = $"{detail.FirstName} {detail.LastName}".Trim();

        return new VenueApprovalDetailBffDto
        {
            UserId = detail.UserId,
            ProfileId = detail.Id,
            Status = MapApprovalStatus(detail.ApprovalStatus),
            ReviewedBy = null,
            ReviewedAt = reviewedAt,
            RejectionCategory = null,
            RejectionReason = detail.RejectReason,
            InternalNote = null,
            Owner = new VenueOwnerBffDto
            {
                FullName = string.IsNullOrEmpty(fullName) ? null : fullName,
                Email = withUser?.Email,
                Phone = withUser?.PhoneNumber,
                AvatarUrl = detail.ProfilePhotoUrl,
                RegisteredAt = withUser?.UserCreatedAt?.ToString("O")
            },
            Venue = new VenueDetailBffDto(),
            Location = new VenueLocationBffDto
            {
                Latitude = null,
                Longitude = null,
                AddressVerified = false,
                DisplayText = null
            },
            Checklist = new ProfileApprovalChecklistBffDto
            {
                EmailVerified = false,
                PhoneVerified = false,
                CompanyNameProvided = false,
                TaxNumberProvided = false,
                DocumentsUploaded = detail.Documents?.Count > 0,
                DuplicateAccountFound = false,
                SuspiciousActivityFound = false
            },
            Documents = detail.Documents?.Select(d => new ProfileApprovalDocumentBffDto
            {
                Id = d.Id.ToString(),
                Type = d.DocumentType,
                Name = d.Name,
                FileId = d.FileId.ToString(),
                Url = signedUrlMap.GetValueOrDefault(d.FileId),
                Format = d.Format,
                Size = d.FileSizeDisplay,
                Issuer = d.Issuer,
                MatchScore = d.MatchScore,
                UploadedAt = d.UploadedAt ?? string.Empty
            }).ToList() ?? new List<ProfileApprovalDocumentBffDto>(),
            RiskSignals = detail.RiskSignals?.Select(r => new ProfileApprovalRiskSignalBffDto
            {
                Level = r.Severity,
                Title = r.Title,
                Description = r.Description
            }).ToList() ?? new List<ProfileApprovalRiskSignalBffDto>(),
            Activity = new List<ProfileApprovalActivityItemBffDto>()
        };
    }

    private static string MapApprovalStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "pending" => "pending",
        "approved" => "approved",
        "rejected" => "rejected",
        _ => "pending"
    };
}
