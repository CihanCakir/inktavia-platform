using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.CompleteCargoDrySupplyOrderBff;

public sealed class CompleteCargoDrySupplyOrderBffCommand : AizenCommand<CompleteCargoDrySupplyOrderBffResponse>
{
    public long ServiceRequestId { get; init; }
}

[DocumentationInfo("Complete CargoDry cargo order (BFF)",
    "Proxies to the ServiceRequest manual cargo-complete endpoint (Shipped → Completed/Closed + revenue recognition).")]
public sealed class CompleteCargoDrySupplyOrderBffCommandHandler
    : AizenCommandHandler<CompleteCargoDrySupplyOrderBffCommand, CompleteCargoDrySupplyOrderBffResponse>
{
    private readonly IServiceRequestRemoteCall _sr;
    public CompleteCargoDrySupplyOrderBffCommandHandler(IServiceRequestRemoteCall sr) => _sr = sr;

    public override async Task<CompleteCargoDrySupplyOrderBffResponse?> Handle(
        CompleteCargoDrySupplyOrderBffCommand request, CancellationToken ct)
    {
        var result = await _sr.CompleteCargoDrySupplyOrder(request.ServiceRequestId);
        return result.Body ?? new CompleteCargoDrySupplyOrderBffResponse { ServiceRequestId = request.ServiceRequestId, Completed = false };
    }
}
