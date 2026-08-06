using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("Get admin vessel documents BFF query handler", "Fetches vessel documents and maps to UI-ready BFF DTOs with computed expiry fields.")]
public sealed class GetVesselDocumentsBffQueryHandler
    : AizenQueryHandler<GetVesselDocumentsBffQuery, AdminVesselDocumentsBffResponse>
{
    private readonly IVesselRemoteCall _vessel;

    public GetVesselDocumentsBffQueryHandler(
        IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<AdminVesselDocumentsBffResponse?> Handle(
        GetVesselDocumentsBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselDocumentsBffResponse();

        try
        {

            var result = await _vessel.GetVesselDocuments(
                request.VesselId,
                pageIndex: 0, pageSize: 200);

            var docs = result?.Body?.Documents?.Items;
            if (docs != null)
            {
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
                        ApprovedAt = d.ApprovedAt,
                        ApprovedByUserId = d.ApprovedByUserId,
                        Notes = d.Notes,
                        IsActive = d.IsActive
                    };
                });

                response.Documents = string.IsNullOrWhiteSpace(request.StatusFilter)
                    ? mapped.ToList()
                    : mapped.Where(d => string.Equals(d.DocumentStatus, request.StatusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
        catch (Exception)
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel.Documents"));
        }

        return response;
    }
}
