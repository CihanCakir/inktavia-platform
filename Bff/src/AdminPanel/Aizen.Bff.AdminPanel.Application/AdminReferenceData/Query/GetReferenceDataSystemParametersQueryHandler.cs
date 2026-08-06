using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataSystemParameters handler", "Forwards admin system parameters request to the ReferenceData module.")]
public sealed class GetReferenceDataSystemParametersQueryHandler
    : AizenQueryHandler<GetReferenceDataSystemParametersQuery, SystemParameterListResult>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetReferenceDataSystemParametersQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<SystemParameterListResult> Handle(GetReferenceDataSystemParametersQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetSystemParameters();
        return new SystemParameterListResult { Items = response.Body?.ToList() };
    }
}
