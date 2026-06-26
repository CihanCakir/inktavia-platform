using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

public sealed class GetReferenceDataLookupItemsByGroupCodeQuery : AizenQuery<LookupItemListResult>
{
    public string GroupCode { get; }

    public GetReferenceDataLookupItemsByGroupCodeQuery(string groupCode)
    {
        GroupCode = groupCode;
    }
}
