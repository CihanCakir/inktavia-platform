using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get admin service request list query handler",
    "Fetches the paged admin service request list, enriched at the BFF with vessel names (Vessel module batch) and assigned-provider display names (Identity batch). Enrichment is best-effort — a failed resolve leaves the name null and the frontend falls back to the id.")]
public sealed class GetServiceRequestListBffQueryHandler
    : AizenQueryHandler<GetServiceRequestListBffQuery, AdminServiceRequestListResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IVesselRemoteCall         _vessel;
    private readonly IIdentityRemoteCall       _identity;

    public GetServiceRequestListBffQueryHandler(
        IServiceRequestRemoteCall serviceRequest,
        IVesselRemoteCall         vessel,
        IIdentityRemoteCall       identity)
    {
        _serviceRequest = serviceRequest;
        _vessel         = vessel;
        _identity       = identity;
    }

    public override async Task<AdminServiceRequestListResponse?> Handle(
        GetServiceRequestListBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestListResponse();

        List<Aizen.Modules.ServiceRequest.Abstraction.Dto.ServiceRequestSummaryDto>? sourceItems;
        int total;
        try
        {
            var result = await _serviceRequest.GetAdminServiceRequestList(
                request.Status, request.VesselId, request.OwnerUserId, request.PageIndex, request.PageSize);

            sourceItems = result?.Body?.Items;
            total = result?.Body?.TotalCount ?? 0;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
            return response;
        }

        if (sourceItems is null)
            return response;

        var items = sourceItems.Select(s => new ServiceRequestListItemBffDto
        {
            Id = s.Id,
            RequestCode = s.RequestCode,
            VesselId = s.VesselId,
            ServiceType = s.ServiceTypeCode ?? s.ServiceCategoryCode,
            ServiceCategoryCode = s.ServiceCategoryCode,
            Title = s.Title,
            Status = s.Status.ToString().ToLowerInvariant(),
            Priority = s.Priority.ToString().ToLowerInvariant(),
            Location = s.LocationMarinaName,
            Notes = s.OwnerNotes ?? s.Title,
            OwnerUserId = s.OwnerUserId,
            ProviderProfileId = s.ProviderProfileId,
            OfferCount = s.OfferCount,
            HasActiveAssignment = s.HasActiveAssignment,
            RequestedDate = s.RequestedStartDate,
            LastActivityAt = s.LastActivityAt,
            CreatedAt = s.CreatedAt
        }).ToList();

        // ── Id→name enrichment at the BFF integration point (best-effort, batched, deduped) ──────────
        // Vessels: most rows share one vessel, so the distinct set is small.
        var distinctVesselIds = items
            .Select(i => i.VesselId)
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        // Providers: only assigned rows carry a provider user id (module list row exposes it now).
        var distinctProviderUserIds = sourceItems
            .Select(s => s.ProviderUserId ?? 0)
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        var vesselNamesTask   = distinctVesselIds.Length > 0       ? FetchVesselNamesAsync(distinctVesselIds, response)             : Task.FromResult(new Dictionary<long, string>());
        var providerNamesTask = distinctProviderUserIds.Length > 0 ? FetchUserNamesAsync(distinctProviderUserIds, response)         : Task.FromResult(new Dictionary<long, string>());

        await Task.WhenAll(vesselNamesTask, providerNamesTask);
        var vesselNames   = await vesselNamesTask;
        var providerNames = await providerNamesTask;

        // Map resolved names onto rows (null stays null → FE falls back to the id).
        for (var idx = 0; idx < items.Count; idx++)
        {
            var row = items[idx];
            if (vesselNames.TryGetValue(row.VesselId, out var vName))
                row.VesselName = vName;

            var providerUserId = sourceItems[idx].ProviderUserId ?? 0;
            if (providerUserId > 0 && providerNames.TryGetValue(providerUserId, out var pName))
                row.ProviderName = pName;
        }

        var pages = request.PageSize > 0 ? (int)Math.Ceiling((double)total / request.PageSize) : 0;

        response.ServiceRequests = new ServiceRequestPageBffDto
        {
            From = request.PageIndex * request.PageSize,
            Index = request.PageIndex,
            Size = request.PageSize,
            Count = total,
            Pages = pages,
            HasPrevious = request.PageIndex > 0,
            HasNext = request.PageIndex < pages - 1,
            Items = items
        };

        return response;
    }

    private async Task<Dictionary<long, string>> FetchVesselNamesAsync(long[] vesselIds, AdminServiceRequestListResponse response)
    {
        try
        {
            var result = await _vessel.GetVesselNamesByIds(vesselIds);
            return (result.Body ?? [])
                .Where(v => !string.IsNullOrWhiteSpace(v.Name))
                .ToDictionary(v => v.VesselId, v => v.Name);
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
            return new Dictionary<long, string>();
        }
    }

    private async Task<Dictionary<long, string>> FetchUserNamesAsync(long[] userIds, AdminServiceRequestListResponse response)
    {
        try
        {
            var result = await _identity.GetUserProfilesByUserIds(userIds);
            return (result.Body ?? [])
                .ToDictionary(
                    p => p.UserId,
                    p => $"{p.FirstName} {p.LastName}".Trim());
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
            return new Dictionary<long, string>();
        }
    }
}
