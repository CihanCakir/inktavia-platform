using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetProviderConversationsBffQuery : AizenQuery<GetProviderConversationsResponse>
{
}
