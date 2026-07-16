namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Get vessel summaries response", "Bulk summary for card decoration. Missing vessel ids are omitted, not errored.")]
public sealed class GetVesselSummariesResponse
{
    public List<VesselSummaryDto> Items { get; init; } = new();
}

public sealed record VesselSummaryDto
{
    public long VesselId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? VesselTypeCode { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public decimal? LengthValue { get; init; }
    public string? LengthUnitCode { get; init; }
}
