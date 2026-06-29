using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryWarehouseOptions;

[DocumentationInfo("Get CargoDry warehouse options BFF query handler",
    "Returns the list of available dispatch warehouse options for the Generate Batch modal.")]
public sealed class GetCargoDryWarehouseOptionsBffQueryHandler
    : AizenQueryHandler<GetCargoDryWarehouseOptionsBffQuery, GetCargoDryWarehouseOptionsBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public GetCargoDryWarehouseOptionsBffQueryHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryWarehouseOptionsBffResponse> Handle(
        GetCargoDryWarehouseOptionsBffQuery request, CancellationToken ct)
    {
        var upstream = await _remote.GetWarehousesAsync(ct);

        var dtos = upstream.Select(w => new CargoDryWarehouseOptionBffDto
        {
            Code         = w.Code,
            Name         = w.Name,
            LocationName = w.LocationName,
            CountryCode  = w.CountryCode,
            IsActive     = w.IsActive,
        }).ToList();

        return new GetCargoDryWarehouseOptionsBffResponse { Warehouses = dtos };
    }
}
