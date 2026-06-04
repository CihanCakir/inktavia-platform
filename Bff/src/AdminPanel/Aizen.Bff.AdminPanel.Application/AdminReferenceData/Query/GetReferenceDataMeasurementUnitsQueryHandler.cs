using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataMeasurementUnits handler", "Forwards admin measurement units request to the ReferenceData module.")]
public sealed class GetReferenceDataMeasurementUnitsQueryHandler
    : AizenQueryHandler<GetReferenceDataMeasurementUnitsQuery, MeasurementUnitListResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;

    public GetReferenceDataMeasurementUnitsQueryHandler(IReferenceDataAdminBffRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<MeasurementUnitListResult> Handle(GetReferenceDataMeasurementUnitsQuery request, CancellationToken ct)
    {
        var response = await _referenceData.GetMeasurementUnits(
            request.Authorization, request.UserToken, request.Type);
        return response.Body ?? new MeasurementUnitListResult();
    }
}
