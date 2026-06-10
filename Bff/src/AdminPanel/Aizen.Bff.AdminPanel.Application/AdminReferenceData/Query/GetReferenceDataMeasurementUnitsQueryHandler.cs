using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataMeasurementUnits handler", "Forwards admin measurement units request to the ReferenceData module.")]
public sealed class GetReferenceDataMeasurementUnitsQueryHandler
    : AizenQueryHandler<GetReferenceDataMeasurementUnitsQuery, MeasurementUnitListResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetReferenceDataMeasurementUnitsQueryHandler(IReferenceDataAdminBffRemoteCall referenceData,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _referenceData = referenceData;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<MeasurementUnitListResult> Handle(GetReferenceDataMeasurementUnitsQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var response = await _referenceData.GetMeasurementUnits(
            authHeader, request.UserToken, request.Type);
        return response.Body ?? new MeasurementUnitListResult();
    }
}
