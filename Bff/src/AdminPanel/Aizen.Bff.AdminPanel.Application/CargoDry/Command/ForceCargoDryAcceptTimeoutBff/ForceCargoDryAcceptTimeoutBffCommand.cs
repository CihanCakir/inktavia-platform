using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.ForceCargoDryAcceptTimeoutBff;

/// <summary>
/// Dev/ops reconciliation — forces the CargoDry-supply accept-timeout fallback for one SR via the ServiceRequest
/// internal route. The AdminPanel BFF controller gates this to non-production environments.
/// </summary>
public sealed class ForceCargoDryAcceptTimeoutBffCommand : AizenCommand<ForceCargoDryAcceptTimeoutResponse>
{
    public long ServiceRequestId { get; init; }
}

[DocumentationInfo("Force CargoDry accept-timeout fallback (BFF)",
    "Dev/ops passthrough to the ServiceRequest internal force-accept-timeout endpoint. Non-production only.")]
public sealed class ForceCargoDryAcceptTimeoutBffCommandHandler
    : AizenCommandHandler<ForceCargoDryAcceptTimeoutBffCommand, ForceCargoDryAcceptTimeoutResponse>
{
    private readonly IServiceRequestRemoteCall _sr;

    public ForceCargoDryAcceptTimeoutBffCommandHandler(IServiceRequestRemoteCall sr) => _sr = sr;

    public override async Task<ForceCargoDryAcceptTimeoutResponse?> Handle(
        ForceCargoDryAcceptTimeoutBffCommand request, CancellationToken ct)
    {
        var result = await _sr.ForceCargoDryAcceptTimeout(
            new ForceCargoDryAcceptTimeoutRequest { ServiceRequestId = request.ServiceRequestId });
        return result.Body ?? new ForceCargoDryAcceptTimeoutResponse { FellBack = false };
    }
}
