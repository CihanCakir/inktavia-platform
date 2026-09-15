using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Application.Commands.AllocateBatchToProvider;
using Aizen.Modules.CargoDry.Application.Common;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using MediatR;

namespace Aizen.Modules.CargoDry.Application.Commands.ApproveProviderStockRequest;

/// <summary>
/// Admin approves a Pending stock request by allocating one batch to the requesting provider. Reuses the CANONICAL
/// <see cref="AllocateBatchToProviderCommand"/> (agreement cap enforced there — allocation logic is NOT forked) and
/// stores the resulting batch code + allocated quantity as the approval's allocation reference. Pending → Approved.
/// </summary>
public sealed class ApproveProviderStockRequestCommand : AizenCommand<CargoDryStockRequestDto>
{
    public long                    RequestId              { get; init; }
    public long                    DecidedByUserId        { get; init; }
    public string?                 DecisionNote           { get; init; }
    // Allocation inputs (threaded straight into AllocateBatchToProvider).
    public string                  BatchCode              { get; init; } = default!;
    public CargoDryCommercialModel CommercialModel        { get; init; } = CargoDryCommercialModel.PrincipalSale;
    public SalesChannel            SalesChannel           { get; init; } = SalesChannel.ConsignmentSellThrough;
    public long?                   ConsignmentAgreementId { get; init; }
    public long?                   WarehouseId            { get; init; }
}

[DocumentationInfo("Approve provider stock request",
    "Allocates a batch to the provider via the canonical AllocateBatchToProvider command, then marks the request Approved.")]
public sealed class ApproveProviderStockRequestCommandHandler
    : AizenCommandHandler<ApproveProviderStockRequestCommand, CargoDryStockRequestDto>
{
    private readonly ICargoDryStockRequestRepository _requests;
    private readonly ISender                         _sender;
    private readonly IAizenMessagePublisher          _messagePublisher;

    public ApproveProviderStockRequestCommandHandler(
        ICargoDryStockRequestRepository requests, ISender sender, IAizenMessagePublisher messagePublisher)
    {
        _requests = requests;
        _sender   = sender;
        _messagePublisher = messagePublisher;
    }

    public override async Task<CargoDryStockRequestDto?> Handle(
        ApproveProviderStockRequestCommand request, CancellationToken ct)
    {
        var entity = await _requests.GetByIdAsync(request.RequestId, ct)
            ?? throw new AizenBusinessException($"Stock request {request.RequestId} not found.");

        if (entity.Status != CargoDryStockRequestStatus.Pending)
            throw new AizenBusinessException($"Only a Pending stock request can be approved (current: {entity.Status}).");

        if (string.IsNullOrWhiteSpace(request.BatchCode))
            throw new AizenBusinessException("A batch code is required to approve a stock request (approval = allocation).");

        // Canonical allocation — agreement cap + inventory/movement handled here; do NOT duplicate.
        var allocation = await _sender.Send(new AllocateBatchToProviderCommand
        {
            BatchCode              = request.BatchCode,
            ProviderProfileId      = entity.ProviderProfileId,
            CommercialModel        = request.CommercialModel,
            SalesChannel           = request.SalesChannel,
            ConsignmentAgreementId = request.ConsignmentAgreementId,
            WarehouseId            = request.WarehouseId,
            Note                   = $"Stock request {entity.RequestCode} approval",
        }, ct)
            ?? throw new AizenBusinessException("Allocation failed while approving the stock request.");

        entity.Approve(request.DecidedByUserId, request.DecisionNote, allocation.BatchCode, allocation.AllocatedCount);
        await _requests.SaveChangesAsync(ct);

        await _messagePublisher.PublishAsync(new CargoDryStockRequestEventMessage
        {
            StockRequestId    = entity.Id,
            RequestCode       = entity.RequestCode,
            ProviderProfileId = entity.ProviderProfileId,
            ProductCode       = entity.ProductCode,
            Event             = CargoDryStockRequestEvent.Approved,
            RequestedQuantity = entity.RequestedQuantity,
        }, ct);

        return entity.ToDto();
    }
}
