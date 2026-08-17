using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

public sealed class GetReferenceDataMeasurementUnitsBffQuery : AizenQuery<MeasurementUnitListResult>
{
    public string? Type { get; }

    public GetReferenceDataMeasurementUnitsBffQuery(string? type)
    {
        Type = type;
    }
}
