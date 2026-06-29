using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryWarehouseOptions;

/// <summary>
/// MVP: Returns a hardcoded list of CargoDry dispatch warehouse options.
/// Post-MVP: Replace with a WarehouseRegistry entity backed by database (separate migration required).
/// </summary>
public sealed class GetCargoDryWarehouseOptionsQueryHandler
    : AizenQueryHandler<GetCargoDryWarehouseOptionsQuery, List<CargoDryWarehouseOptionDto>>
{
    private static readonly List<CargoDryWarehouseOptionDto> StaticWarehouses =
    [
        new() { Code = "SGP-MAIN", Name = "Singapore Main",    LocationName = "Singapore", CountryCode = "SGP", IsActive = true },
        new() { Code = "UAE-DXB",  Name = "Dubai Hub",         LocationName = "Dubai",     CountryCode = "UAE", IsActive = true },
        new() { Code = "GRC-PIR",  Name = "Athens Piraeus",    LocationName = "Piraeus",   CountryCode = "GRC", IsActive = true },
        new() { Code = "TUR-IST",  Name = "Istanbul Maritime", LocationName = "Istanbul",  CountryCode = "TUR", IsActive = true },
        new() { Code = "ESP-BCN",  Name = "Barcelona Port",    LocationName = "Barcelona", CountryCode = "ESP", IsActive = true },
        new() { Code = "HRV-SPL",  Name = "Split Adriatic",    LocationName = "Split",     CountryCode = "HRV", IsActive = true },
        new() { Code = "MLT-VLT",  Name = "Valletta Malta",    LocationName = "Valletta",  CountryCode = "MLT", IsActive = true },
        new() { Code = "MNE-TIV",  Name = "Tivat Montenegro",  LocationName = "Tivat",     CountryCode = "MNE", IsActive = true },
    ];

    public override Task<List<CargoDryWarehouseOptionDto>> Handle(
        GetCargoDryWarehouseOptionsQuery request, CancellationToken ct)
        => Task.FromResult(StaticWarehouses);
}
