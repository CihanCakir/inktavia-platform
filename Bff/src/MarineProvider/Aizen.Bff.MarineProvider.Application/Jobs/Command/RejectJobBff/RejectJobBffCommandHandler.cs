using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class RejectJobBffCommandHandler : AizenCommandHandler<RejectJobBffCommand, JobSuccessResult>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly IServiceRequestRemoteCall _sr;

    public RejectJobBffCommandHandler(IProviderProfileResolver r, IProviderIdentityHolder h, IServiceRequestRemoteCall sr)
    { _resolver = r; _h = h; _sr = sr; }

    public override async Task<JobSuccessResult?> Handle(RejectJobBffCommand cmd, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        try { await _sr.RejectJob(cmd.AssignmentId, cmd.Body); }
        catch (Refit.ApiException ex) { throw new AizenBusinessException(RefitErrorHelper.ExtractError(ex) ?? "Failed to reject job."); }
        return new JobSuccessResult();
    }
}
