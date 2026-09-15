using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.MarkCargoDrySupplyShippedBff;

public sealed class MarkCargoDrySupplyShippedBffCommand : AizenCommand<MarkCargoDrySupplyShippedBffResponse>
{
    public long    ServiceRequestId { get; init; }
    public string  TrackingCode     { get; init; } = default!;
    public long?   KitId            { get; init; }
}

[DocumentationInfo("Mark CargoDry cargo order shipped (BFF)",
    "Proxies to the ServiceRequest cargo ship endpoint (AwaitingShipment → Shipped + tracking code).")]
public sealed class MarkCargoDrySupplyShippedBffCommandHandler
    : AizenCommandHandler<MarkCargoDrySupplyShippedBffCommand, MarkCargoDrySupplyShippedBffResponse>
{
    private readonly IServiceRequestRemoteCall _sr;
    public MarkCargoDrySupplyShippedBffCommandHandler(IServiceRequestRemoteCall sr) => _sr = sr;

    public override async Task<MarkCargoDrySupplyShippedBffResponse?> Handle(
        MarkCargoDrySupplyShippedBffCommand request, CancellationToken ct)
    {
        var result = await _sr.MarkCargoDrySupplyShipped(request.ServiceRequestId,
            new MarkCargoDrySupplyShippedRequest { TrackingCode = request.TrackingCode, KitId = request.KitId });
        return result.Body ?? new MarkCargoDrySupplyShippedBffResponse { ServiceRequestId = request.ServiceRequestId, TrackingCode = request.TrackingCode };
    }
}
