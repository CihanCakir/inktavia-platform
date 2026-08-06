using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

public sealed class GetReferenceDataLookupGroupTreeBffQuery : AizenQuery<LookupGroupTreeResult>
{

    public GetReferenceDataLookupGroupTreeBffQuery()
    {
    }
}
