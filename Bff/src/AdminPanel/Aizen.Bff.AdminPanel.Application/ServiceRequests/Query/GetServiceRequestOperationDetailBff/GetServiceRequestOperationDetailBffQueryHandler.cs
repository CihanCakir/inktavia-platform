using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get admin service request operation detail query handler",
    "Fetches the full service request detail for the admin operation panel, enriched with vessel name, provider display names and presigned attachment/evidence media URLs.")]
public sealed class GetServiceRequestOperationDetailBffQueryHandler
    : AizenQueryHandler<GetServiceRequestOperationDetailBffQuery, AdminServiceRequestOperationDetailResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IVesselRemoteCall         _vessel;
    private readonly IIdentityRemoteCall       _identity;
    private readonly IFileStorageRemoteCall    _fileStorage;

    public GetServiceRequestOperationDetailBffQueryHandler(
        IServiceRequestRemoteCall serviceRequest,
        IVesselRemoteCall         vessel,
        IIdentityRemoteCall       identity,
        IFileStorageRemoteCall    fileStorage)
    {
        _serviceRequest = serviceRequest;
        _vessel         = vessel;
        _identity       = identity;
        _fileStorage    = fileStorage;
    }

    public override async Task<AdminServiceRequestOperationDetailResponse?> Handle(
        GetServiceRequestOperationDetailBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestOperationDetailResponse();

        try
        {
            var result = await _serviceRequest.GetAdminServiceRequestDetail(request.ServiceRequestId);
            response.ServiceRequest = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
            return response;
        }

        var detail = response.ServiceRequest?.Detail;
        if (detail is null)
            return response;

        // ── Collect IDs for parallel enrichment ──────────────────────────────
        var vesselId = detail.Request?.VesselId ?? 0;
        var ownerUserId = detail.Request?.OwnerUserId ?? 0;

        var providerUserIds = (detail.Offers ?? [])
            .Select(o => o.ProviderUserId)
            .Concat(detail.Assignment is not null ? [detail.Assignment.ProviderUserId] : Array.Empty<long>())
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        // Owner + providers resolve in ONE identity batch (no extra round-trip).
        var identityUserIds = providerUserIds
            .Concat(ownerUserId > 0 ? [ownerUserId] : Array.Empty<long>())
            .Distinct()
            .ToArray();

        // ── Parallel calls — failures are swallowed, frontend falls back to IDs ──
        var vesselTask   = vesselId > 0               ? FetchVesselNameAsync(vesselId, cancellationToken)           : Task.FromResult<string?>(null);
        var identityTask = identityUserIds.Length > 0 ? FetchProviderNamesAsync(identityUserIds, cancellationToken) : Task.FromResult(new Dictionary<long, string>());

        await Task.WhenAll(vesselTask, identityTask);

        response.VesselName = await vesselTask;
        var names = await identityTask;
        response.ProviderNames = names;
        response.OwnerName = ownerUserId > 0 && names.TryGetValue(ownerUserId, out var ownerName) ? ownerName : null;

        // ── Presign every uploaded file on the SR so the admin can view it (best-effort, never 500) ──
        response.Media = await ResolveMediaAsync(detail, response);

        return response;
    }

    /// <summary>
    /// Collects every file on the SR — request attachments, provider completion evidence and work-log evidence photos —
    /// and resolves each into a viewable presigned URL (+ mime/isImage/fileName from FileStorage metadata). Best-effort
    /// per file: a presign/metadata failure leaves that item's <c>Url</c>/<c>MimeType</c> null and appends a single
    /// FileStorage warning, so the endpoint never 500s. Dispute has no evidence file in the SR module; message images
    /// are served (already presigned) by the Messaging module and rendered directly by the admin message view.
    /// </summary>
    private async Task<List<AdminSrMediaFileDto>> ResolveMediaAsync(
        Aizen.Modules.ServiceRequest.Abstraction.Dto.ServiceRequestDetailDto detail,
        AdminServiceRequestOperationDetailResponse response)
    {
        // Build the surface refs first (a file id + where it came from), then presign the DISTINCT ids once.
        var refs = new List<AdminSrMediaFileDto>();

        foreach (var att in detail.Attachments ?? [])
            refs.Add(new AdminSrMediaFileDto
            {
                FileId    = att.FileId,
                Surface   = AdminSrMediaSurfaces.RequestAttachment,
                Title     = att.Title,
                SourceId  = att.Id,
                CreatedAt = att.CreatedAt,
            });

        if (detail.Completion?.EvidenceFileId is { } evidenceId && evidenceId != Guid.Empty)
            refs.Add(new AdminSrMediaFileDto
            {
                FileId    = evidenceId,
                Surface   = AdminSrMediaSurfaces.CompletionEvidence,
                SourceId  = detail.Completion.Id,
                CreatedAt = detail.Completion.SubmittedAt,
            });

        foreach (var log in detail.WorkLogs ?? [])
            if (log.AttachmentFileId is { } logFileId && logFileId != Guid.Empty)
                refs.Add(new AdminSrMediaFileDto
                {
                    FileId    = logFileId,
                    Surface   = AdminSrMediaSurfaces.WorkLogPhoto,
                    Title     = log.Title,
                    SourceId  = log.Id,
                    CreatedAt = log.LoggedAt,
                });

        if (refs.Count == 0)
            return refs;

        var distinctIds = refs.Select(r => r.FileId).Distinct().ToArray();
        var resolved    = await ResolveFilesAsync(distinctIds, response);

        foreach (var r in refs)
        {
            if (resolved.TryGetValue(r.FileId, out var f))
            {
                r.Url      = f.Url;
                r.MimeType = f.MimeType;
                r.FileName = f.FileName;
                r.IsImage  = f.MimeType is { Length: > 0 } m
                             && m.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            }
        }

        return refs;
    }

    private readonly record struct ResolvedFile(string? Url, string? MimeType, string? FileName);

    /// <summary>
    /// Presigns each file id and fetches its metadata (content type + original name) from FileStorage — both calls in
    /// one parallel best-effort pass, mirroring the vessel <c>PresignAsync</c> helper. A single warning is appended if
    /// any presign came back empty; individual failures never throw.
    /// </summary>
    private async Task<Dictionary<Guid, ResolvedFile>> ResolveFilesAsync(
        Guid[] fileIds, AdminServiceRequestOperationDetailResponse response)
    {
        var map = new Dictionary<Guid, ResolvedFile>();
        if (fileIds.Length == 0)
            return map;

        var req = new CreateFileReadUrlRemoteCallRequest { ExpiresIn = TimeSpan.FromMinutes(60) };
        var entries = fileIds
            .Select(id => (id, url: _fileStorage.CreateReadUrl(id, req), meta: _fileStorage.GetFileMetadata(id)))
            .ToList();

        await Task.WhenAll(entries.SelectMany(e => new Task[]
        {
            e.url.ContinueWith(_ => { }),
            e.meta.ContinueWith(_ => { }),
        }));

        var anyFailed = false;
        foreach (var (id, urlTask, metaTask) in entries)
        {
            var url  = urlTask.IsCompletedSuccessfully ? urlTask.Result.Body?.ReadUrl : null;
            var meta = metaTask.IsCompletedSuccessfully ? metaTask.Result.Body : null;
            if (string.IsNullOrEmpty(url))
                anyFailed = true;
            map[id] = new ResolvedFile(
                string.IsNullOrEmpty(url) ? null : url,
                meta?.ContentType,
                meta?.OriginalFileName);
        }

        if (anyFailed)
            response.Warnings.Add(AdminBffWarning.CallFailed("FileStorage", "Some file URLs could not be presigned."));

        return map;
    }

    private async Task<string?> FetchVesselNameAsync(long vesselId, CancellationToken ct)
    {
        try
        {
            var result = await _vessel.GetVesselById(vesselId);
            return result.Body?.Vessel?.Vessel?.Name;
        }
        catch { return null; }
    }

    private async Task<Dictionary<long, string>> FetchProviderNamesAsync(long[] userIds, CancellationToken ct)
    {
        try
        {
            var result = await _identity.GetUserProfilesByUserIds(userIds);
            return (result.Body ?? [])
                .ToDictionary(
                    p => p.UserId,
                    p => $"{p.FirstName} {p.LastName}".Trim());
        }
        catch { return new Dictionary<long, string>(); }
    }
}
