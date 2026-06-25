using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

public sealed class GetReferenceDataLookupGroupTreeQuery : AizenQuery<LookupGroupTreeResult>
{
    public string UserToken { get; }

    public GetReferenceDataLookupGroupTreeQuery(string userToken)
    {
        UserToken = userToken;
    }
}
