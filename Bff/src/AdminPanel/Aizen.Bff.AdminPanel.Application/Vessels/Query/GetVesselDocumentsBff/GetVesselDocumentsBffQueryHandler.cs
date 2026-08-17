using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("Get admin vessel documents BFF query handler",
    "Fetches vessel documents (presigned file URLs from the module), maps to UI-ready BFF DTOs with computed expiry fields, and resolves the approver display name via Identity (best-effort).")]
public sealed class GetVesselDocumentsBffQueryHandler
    : AizenQueryHandler<GetVesselDocumentsBffQuery, AdminVesselDocumentsBffResponse>
{
    private readonly IVesselRemoteCall _vessel;
    private readonly IIdentityRemoteCall _identity;

    public GetVesselDocumentsBffQueryHandler(
        IVesselRemoteCall vessel,
        IIdentityRemoteCall identity)
    {
        _vessel = vessel;
        _identity = identity;
    }

    public override async Task<AdminVesselDocumentsBffResponse?> Handle(
        GetVesselDocumentsBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselDocumentsBffResponse();

        try
        {
            // R5 — ask the Vessel module to presign each document file (it builds the read URL in-module).
            var result = await _vessel.GetVesselDocuments(
                request.VesselId,
                pageIndex: 0, pageSize: 200,
                includeAccessUrls: true);

            var docs = result?.Body?.Documents?.Items;
            if (docs == null)
                return response;

            var mapped = docs.Select(d =>
            {
                var daysUntilExpiry = d.ExpiresAt.HasValue
                    ? (int?)(d.ExpiresAt.Value - DateTime.UtcNow).TotalDays
                    : null;

                return new VesselDocumentBffDto
                {
                    Id = d.Id,
                    DocumentType = d.DocumentTypeCode,
                    DocumentCategory = d.DocumentCategory,
                    ExpiryDate = d.ExpiresAt,
                    DaysUntilExpiry = daysUntilExpiry,
                    IssuingAuthority = d.IssuingAuthority,
                    DocumentStatus = d.Status.ToString(),
                    OriginalFileName = d.OriginalFileNameSnapshot,
                    ContentType = d.ContentTypeSnapshot,
                    FileSizeBytes = d.SizeInBytesSnapshot,
                    FileUrl = d.AccessUrl,
                    ThumbnailUrl = d.AccessUrl,
                    ApprovedAt = d.ApprovedAt,
                    ApprovedByUserId = d.ApprovedByUserId,
                    Notes = d.Notes,
                    IsActive = d.IsActive,
                    Versions = new(),
                };
            }).ToList();

            // Resolve approver display names in one Identity batch (best-effort — a failure leaves names null).
            var approverIds = mapped
                .Where(d => d.ApprovedByUserId.HasValue && d.ApprovedByUserId.Value > 0)
                .Select(d => d.ApprovedByUserId!.Value)
                .Distinct()
                .ToArray();

            if (approverIds.Length > 0)
            {
                try
                {
                    var profiles = await _identity.GetUserProfilesByUserIds(approverIds);
                    if (profiles?.Header?.IsSuccess == true && profiles.Body is { Count: > 0 })
                    {
                        var nameByUserId = profiles.Body
                            .Where(p => p.UserId > 0)
                            .GroupBy(p => p.UserId)
                            .ToDictionary(g => g.Key, g => $"{g.First().FirstName} {g.First().LastName}".Trim());

                        foreach (var d in mapped)
                        {
                            if (d.ApprovedByUserId is long uid && nameByUserId.TryGetValue(uid, out var name) && !string.IsNullOrWhiteSpace(name))
                                d.ApprovedByName = name;
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
            }

            response.Documents = string.IsNullOrWhiteSpace(request.StatusFilter)
                ? mapped
                : mapped.Where(d => string.Equals(d.DocumentStatus, request.StatusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception)
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel.Documents"));
        }

        return response;
    }
}
