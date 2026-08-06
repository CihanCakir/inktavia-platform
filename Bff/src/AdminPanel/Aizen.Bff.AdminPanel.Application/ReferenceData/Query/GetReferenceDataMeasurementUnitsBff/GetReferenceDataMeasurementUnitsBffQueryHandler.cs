using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

[DocumentationInfo("GetReferenceDataMeasurementUnits handler", "Forwards admin measurement units request to the ReferenceData module.")]
public sealed class GetReferenceDataMeasurementUnitsBffQueryHandler
    : AizenQueryHandler<GetReferenceDataMeasurementUnitsBffQuery, MeasurementUnitListResult>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetReferenceDataMeasurementUnitsBffQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<MeasurementUnitListResult> Handle(GetReferenceDataMeasurementUnitsBffQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetMeasurementUnits(
request.Type);
        return new MeasurementUnitListResult { Items = response.Body?.ToList() };
    }
}
