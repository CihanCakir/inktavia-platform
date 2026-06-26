using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

public sealed class GetReferenceDataLookupGroupTreeQuery : AizenQuery<LookupGroupTreeResult>
{

    public GetReferenceDataLookupGroupTreeQuery()
    {
    }
}
