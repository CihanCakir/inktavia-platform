using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

public sealed class GetReferenceDataMeasurementUnitsQuery : AizenQuery<MeasurementUnitListResult>
{
    public string? Type { get; }
    public string UserToken { get; }

    public GetReferenceDataMeasurementUnitsQuery(string? type, string userToken)
    {
        Type = type;
        UserToken = userToken;
    }
}
