using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

public sealed class GetReferenceDataLookupItemsByGroupCodeQuery : AizenQuery<LookupItemListResult>
{
    public string GroupCode { get; }
    public string Authorization { get; }
    public string UserToken { get; }

    public GetReferenceDataLookupItemsByGroupCodeQuery(string groupCode, string authorization, string userToken)
    {
        GroupCode = groupCode;
        Authorization = authorization;
        UserToken = userToken;
    }
}
