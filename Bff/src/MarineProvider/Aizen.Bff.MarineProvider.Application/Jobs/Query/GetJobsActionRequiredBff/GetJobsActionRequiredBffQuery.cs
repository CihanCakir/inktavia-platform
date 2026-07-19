using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class GetJobsActionRequiredBffQuery : AizenQuery<GetProviderJobsActionRequiredResponse>
{
}
