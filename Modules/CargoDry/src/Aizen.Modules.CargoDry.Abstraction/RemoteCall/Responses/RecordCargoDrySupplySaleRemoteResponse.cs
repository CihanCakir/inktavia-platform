namespace Aizen.Modules.CargoDry.Abstraction.RemoteCall.Responses;

public sealed class RecordCargoDrySupplySaleRemoteResponse
{
    public bool    Recorded             { get; init; }
    public long?   AttributionId        { get; init; }
    public bool    PreferredProviderSet { get; init; }
    public string? Note                 { get; init; }
}
