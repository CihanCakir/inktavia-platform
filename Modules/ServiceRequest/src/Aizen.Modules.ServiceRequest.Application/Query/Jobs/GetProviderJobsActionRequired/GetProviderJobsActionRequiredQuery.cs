using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

namespace Aizen.Modules.ServiceRequest.Application.Query.Jobs;

public sealed class GetProviderJobsActionRequiredQuery : AizenQuery<GetProviderJobsActionRequiredResponse> { }
