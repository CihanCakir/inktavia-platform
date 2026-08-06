using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

public sealed class GetReferenceDataLookupItemsByGroupCodeBffQuery : AizenQuery<LookupItemListResult>
{
    public string GroupCode { get; }

    public GetReferenceDataLookupItemsByGroupCodeBffQuery(string groupCode)
    {
        GroupCode = groupCode;
    }
}
