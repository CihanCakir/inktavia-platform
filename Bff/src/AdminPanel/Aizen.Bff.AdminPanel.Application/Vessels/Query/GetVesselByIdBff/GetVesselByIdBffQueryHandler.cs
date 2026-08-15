using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("GetAdminVesselById query handler",
    "Returns full vessel detail by ID, enriched with owner display names (Identity, best-effort) and presigned media/document URLs (FileStorage, best-effort). Never a 500.")]
public sealed class GetVesselByIdBffQueryHandler : AizenQueryHandler<GetVesselByIdBffQuery, AdminVesselByIdBffResponse>
{
    private readonly IVesselRemoteCall _vessel;
    private readonly IIdentityRemoteCall _identity;
    private readonly IFileStorageRemoteCall _fileStorage;

    public GetVesselByIdBffQueryHandler(
        IVesselRemoteCall vessel,
        IIdentityRemoteCall identity,
        IFileStorageRemoteCall fileStorage)
    {
        _vessel = vessel;
        _identity = identity;
        _fileStorage = fileStorage;
    }

    public override async Task<AdminVesselByIdBffResponse?> Handle(GetVesselByIdBffQuery request, CancellationToken ct)
    {
        var response = new AdminVesselByIdBffResponse();

        var detail = (await _vessel.GetVesselById(request.VesselId)).Body?.Vessel;
        if (detail is null)
        {
            response.Warnings.Add(AdminBffWarning.CallFailed("Vessel.Detail", "Could not retrieve vessel detail."));
            return response;
        }

        // ── Presign every media + document file (FileStorage, best-effort per item) ────────────────
        var fileIds = detail.Media.Where(m => m.FileId.HasValue).Select(m => m.FileId!.Value)
            .Concat(detail.Documents.Where(d => d.FileId.HasValue).Select(d => d.FileId!.Value))
            .Distinct()
            .ToArray();
        var urlByFileId = await PresignAsync(fileIds, response);

        // ── Resolve owner + approver display names in ONE Identity batch (best-effort) ─────────────
        var userIds = detail.Owners.Select(o => o.UserId)
            .Concat(detail.Documents.Where(d => d.ApprovedByUserId.HasValue && d.ApprovedByUserId.Value > 0)
                .Select(d => d.ApprovedByUserId!.Value))
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        var (nameByUserId, avatarByUserId, emailByUserId) = await ResolveIdentityAsync(userIds, response);

        response.Vessel = new VesselDetailEnrichedBffDto
        {
            Vessel = detail.Vessel,
            Specification = detail.Specification,
            Engines = detail.Engines,
            CurrentLocation = detail.CurrentLocation,
            StatusHistory = detail.StatusHistory,
            Owners = detail.Owners.Select(o => new VesselOwnerEnrichedBffDto
            {
                Id = o.Id,
                VesselId = o.VesselId,
                UserId = o.UserId,
                UserProfileId = o.UserProfileId,
                Role = o.Role,
                Status = o.Status,
                IsPrimary = o.IsPrimary,
                InvitedAt = o.InvitedAt,
                AcceptedAt = o.AcceptedAt,
                RemovedAt = o.RemovedAt,
                Name = nameByUserId.GetValueOrDefault(o.UserId),
                Email = emailByUserId.GetValueOrDefault(o.UserId),
                AvatarUrl = avatarByUserId.GetValueOrDefault(o.UserId),
            }).ToList(),
            Media = detail.Media.Select(m => MapMedia(m, urlByFileId)).ToList(),
            Documents = detail.Documents.Select(d => MapDocument(d, urlByFileId, nameByUserId)).ToList(),
        };

        return response;
    }

    private async Task<Dictionary<Guid, string>> PresignAsync(Guid[] fileIds, AdminVesselByIdBffResponse response)
    {
        var map = new Dictionary<Guid, string>();
        if (fileIds.Length == 0) return map;

        var req = new CreateFileReadUrlRemoteCallRequest { ExpiresIn = TimeSpan.FromMinutes(60) };
        var tasks = fileIds.Select(id => (id, task: _fileStorage.CreateReadUrl(id, req))).ToList();
        await Task.WhenAll(tasks.Select(t => t.task.ContinueWith(_ => { })));

        var anyFailed = false;
        foreach (var (id, task) in tasks)
        {
            var url = task.IsCompletedSuccessfully ? task.Result.Body?.ReadUrl : null;
            if (!string.IsNullOrEmpty(url)) map[id] = url;
            else anyFailed = true;
        }
        if (anyFailed)
            response.Warnings.Add(AdminBffWarning.CallFailed("FileStorage", "Some file URLs could not be presigned."));
        return map;
    }

    private async Task<(Dictionary<long, string> Names, Dictionary<long, string> Avatars, Dictionary<long, string> Emails)>
        ResolveIdentityAsync(long[] userIds, AdminVesselByIdBffResponse response)
    {
        var names = new Dictionary<long, string>();
        var avatars = new Dictionary<long, string>();
        var emails = new Dictionary<long, string>();
        if (userIds.Length == 0) return (names, avatars, emails);

        try
        {
            var profiles = await _identity.GetUserProfilesByUserIds(userIds);
            if (profiles?.Header?.IsSuccess == true && profiles.Body is { Count: > 0 })
            {
                foreach (var p in profiles.Body.Where(p => p.UserId > 0).GroupBy(p => p.UserId).Select(g => g.First()))
                {
                    var full = $"{p.FirstName} {p.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(full)) names[p.UserId] = full;
                    if (!string.IsNullOrWhiteSpace(p.ProfilePhotoUrl)) avatars[p.UserId] = p.ProfilePhotoUrl!;
                    if (!string.IsNullOrWhiteSpace(p.Email)) emails[p.UserId] = p.Email!;
                }
            }
            else
            {
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
            }
        }
        catch (Exception)
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }
        return (names, avatars, emails);
    }

    private static VesselMediaBffDto MapMedia(VesselMediaDto m, Dictionary<Guid, string> urlByFileId)
    {
        var url = m.FileId.HasValue ? urlByFileId.GetValueOrDefault(m.FileId.Value) : null;
        return new VesselMediaBffDto
        {
            Id = m.Id,
            // FE contract is 'image' | 'video' (VesselMediaDto.mediaType). Emitting the raw enum name ("Photo")
            // broke strict consumers like the detail hero (mediaType === 'image'); the gallery only rendered because
            // it falls back to thumbnailUrl. Map Video → 'video', every other media kind → 'image'.
            MediaType = m.MediaType == Aizen.Modules.Vessel.Abstraction.Enum.VesselMediaType.Video ? "video" : "image",
            Title = m.Title,
            Description = m.Description,
            OriginalFileName = m.OriginalFileNameSnapshot,
            ContentType = m.ContentTypeSnapshot,
            FileSizeBytes = m.SizeInBytesSnapshot,
            Url = url,
            ThumbnailUrl = url ?? m.ThumbnailUrl,
            UploadedAt = null,
            UploadedByUserId = m.UploadedByUserId,
            IsPrimary = m.IsCover,
            SortOrder = m.SortOrder,
            IsActive = m.IsActive,
        };
    }

    private static VesselDocumentBffDto MapDocument(
        VesselDocumentDto d, Dictionary<Guid, string> urlByFileId, Dictionary<long, string> nameByUserId)
    {
        var url = d.FileId.HasValue ? urlByFileId.GetValueOrDefault(d.FileId.Value) : null;
        return new VesselDocumentBffDto
        {
            Id = d.Id,
            DocumentType = d.DocumentTypeCode,
            DocumentCategory = d.DocumentCategory,
            ExpiryDate = d.ExpiresAt,
            DaysUntilExpiry = d.ExpiresAt.HasValue ? (int?)(d.ExpiresAt.Value - DateTime.UtcNow).TotalDays : null,
            IssuingAuthority = d.IssuingAuthority,
            DocumentStatus = d.Status.ToString(),
            OriginalFileName = d.OriginalFileNameSnapshot,
            ContentType = d.ContentTypeSnapshot,
            FileSizeBytes = d.SizeInBytesSnapshot,
            FileUrl = url,
            ThumbnailUrl = url,
            ApprovedAt = d.ApprovedAt,
            ApprovedByUserId = d.ApprovedByUserId,
            ApprovedByName = d.ApprovedByUserId is long uid ? nameByUserId.GetValueOrDefault(uid) : null,
            Notes = d.Notes,
            IsActive = d.IsActive,
            Versions = new(),
        };
    }
}
