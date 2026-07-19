using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class GetJobWorkLogsBffQueryHandler
    : AizenQueryHandler<GetJobWorkLogsBffQuery, GetServiceRequestWorkLogsResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly IServiceRequestRemoteCall _sr;

    public GetJobWorkLogsBffQueryHandler(IProviderProfileResolver r, IProviderIdentityHolder h, IServiceRequestRemoteCall sr)
    { _resolver = r; _h = h; _sr = sr; }

    public override async Task<GetServiceRequestWorkLogsResponse?> Handle(GetJobWorkLogsBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        try { return (await _sr.GetWorkLogs(q.AssignmentId)).Body; }
        catch (Refit.ApiException ex) { throw new AizenBusinessException(RefitErrorHelper.ExtractError(ex) ?? "Job not found."); }
    }
}
