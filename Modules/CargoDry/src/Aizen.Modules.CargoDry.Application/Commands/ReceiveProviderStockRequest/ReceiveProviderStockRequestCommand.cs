using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Common;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.ReceiveProviderStockRequest;

/// <summary>
/// Confirms receipt of a Shipped stock request. Provider-driven (portal) when <see cref="ProviderProfileId"/> is set
/// (ownership-checked); system/auto path (the sweep) passes null. Shipped → Received.
/// </summary>
public sealed class ReceiveProviderStockRequestCommand : AizenCommand<CargoDryStockRequestDto>
{
    public long   RequestId         { get; init; }
    /// <summary>The confirming provider (portal path); ownership is verified. Null for the system auto-receive sweep.</summary>
    public long?  ProviderProfileId { get; init; }
    public long?  ReceivedByUserId  { get; init; }
}

[DocumentationInfo("Receive provider stock request", "Marks a Shipped stock request Received (provider-confirmed or auto-swept).")]
public sealed class ReceiveProviderStockRequestCommandHandler
    : AizenCommandHandler<ReceiveProviderStockRequestCommand, CargoDryStockRequestDto>
{
    private readonly ICargoDryStockRequestRepository _requests;

    public ReceiveProviderStockRequestCommandHandler(ICargoDryStockRequestRepository requests) => _requests = requests;

    public override async Task<CargoDryStockRequestDto?> Handle(
        ReceiveProviderStockRequestCommand request, CancellationToken ct)
    {
        var entity = await _requests.GetByIdAsync(request.RequestId, ct)
            ?? throw new AizenBusinessException($"Stock request {request.RequestId} not found.");

        // Provider path: the request must belong to the confirming provider (mirrors Cancel's ownership check).
        if (request.ProviderProfileId is { } pid && entity.ProviderProfileId != pid)
            throw new AizenBusinessException($"Stock request {request.RequestId} not found.");

        if (entity.Status != CargoDryStockRequestStatus.Shipped)
            throw new AizenBusinessException($"Only a Shipped stock request can be received (current: {entity.Status}).");

        entity.Receive(request.ReceivedByUserId);
        await _requests.SaveChangesAsync(ct);

        return entity.ToDto();
    }
}
