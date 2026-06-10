using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

public sealed class GetReferenceDataLookupGroupsQuery : AizenQuery<LookupGroupListResult>
{
    public string UserToken { get; }

    public GetReferenceDataLookupGroupsQuery(string userToken)
    {
        UserToken = userToken;
    }
}
