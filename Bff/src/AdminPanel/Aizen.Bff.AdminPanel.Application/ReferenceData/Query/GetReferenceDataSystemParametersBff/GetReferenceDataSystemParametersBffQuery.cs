using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

public sealed class GetReferenceDataSystemParametersBffQuery : AizenQuery<SystemParameterListResult>
{

    public GetReferenceDataSystemParametersBffQuery()
    {
    }
}
