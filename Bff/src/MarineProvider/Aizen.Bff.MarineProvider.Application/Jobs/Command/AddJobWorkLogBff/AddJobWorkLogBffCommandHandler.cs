using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class AddJobWorkLogBffCommandHandler
    : AizenCommandHandler<AddJobWorkLogBffCommand, AddServiceRequestWorkLogResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly IServiceRequestRemoteCall _sr;

    public AddJobWorkLogBffCommandHandler(IProviderProfileResolver r, IProviderIdentityHolder h, IServiceRequestRemoteCall sr)
    { _resolver = r; _h = h; _sr = sr; }

    public override async Task<AddServiceRequestWorkLogResponse?> Handle(AddJobWorkLogBffCommand cmd, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        try { return (await _sr.AddWorkLog(cmd.AssignmentId, cmd.Body)).Body; }
        catch (Refit.ApiException ex) { throw new AizenBusinessException(RefitErrorHelper.ExtractError(ex) ?? "Failed to add work log."); }
    }
}
