using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetServiceRequestStatsBffQuery : AizenQuery<ServiceRequestStatsBffDto>
{
}
