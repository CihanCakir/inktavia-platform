using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeKit;

public sealed class RevokeKitCommandHandler : AizenCommandHandler<RevokeKitCommand, bool>
{
    private readonly ICargoDryKitRepository _kits;
    private readonly IAizenMessagePublisher _publisher;

    public RevokeKitCommandHandler(ICargoDryKitRepository kits, IAizenMessagePublisher publisher)
    {
        _kits      = kits;
        _publisher = publisher;
    }

    public override async Task<bool> Handle(RevokeKitCommand request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new InvalidOperationException($"Kit {request.KitId} not found");

        kit.Revoke(request.Reason);
        await _kits.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new CargoDryKitRevokedMessage
        {
            KitId       = kit.Id,
            KitCode     = kit.KitCode,
            OwnerUserId = kit.OwnerUserId,
            VesselId    = kit.VesselId,
            Reason      = request.Reason,
        }, ct);

        return true;
    }
}
