using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Application.Common;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.RejectProviderStockRequest;

/// <summary>Admin rejects a Pending stock request with a reason (surfaced verbatim to the provider). Pending → Rejected.</summary>
public sealed class RejectProviderStockRequestCommand : AizenCommand<CargoDryStockRequestDto>
{
    public long   RequestId       { get; init; }
    public long   DecidedByUserId { get; init; }
    public string Reason          { get; init; } = default!;
}

[DocumentationInfo("Reject provider stock request", "Marks a Pending stock request Rejected with a verbatim reason.")]
public sealed class RejectProviderStockRequestCommandHandler
    : AizenCommandHandler<RejectProviderStockRequestCommand, CargoDryStockRequestDto>
{
    private readonly ICargoDryStockRequestRepository _requests;
    private readonly IAizenMessagePublisher          _messagePublisher;

    public RejectProviderStockRequestCommandHandler(
        ICargoDryStockRequestRepository requests, IAizenMessagePublisher messagePublisher)
    {
        _requests = requests;
        _messagePublisher = messagePublisher;
    }

    public override async Task<CargoDryStockRequestDto?> Handle(
        RejectProviderStockRequestCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new AizenBusinessException("A rejection reason is required.");

        var entity = await _requests.GetByIdAsync(request.RequestId, ct)
            ?? throw new AizenBusinessException($"Stock request {request.RequestId} not found.");

        if (entity.Status != CargoDryStockRequestStatus.Pending)
            throw new AizenBusinessException($"Only a Pending stock request can be rejected (current: {entity.Status}).");

        entity.Reject(request.DecidedByUserId, request.Reason.Trim());
        await _requests.SaveChangesAsync(ct);

        await _messagePublisher.PublishAsync(new CargoDryStockRequestEventMessage
        {
            StockRequestId    = entity.Id,
            RequestCode       = entity.RequestCode,
            ProviderProfileId = entity.ProviderProfileId,
            ProductCode       = entity.ProductCode,
            Event             = CargoDryStockRequestEvent.Rejected,
            Reason            = entity.DecisionNote,
        }, ct);

        return entity.ToDto();
    }
}
