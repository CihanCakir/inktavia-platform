using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall;
using Aizen.Modules.CargoDry.Application.Common;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.CargoDry.Application.Commands.ShipProviderStockRequest;

/// <summary>Admin ships an Approved stock request (tracking code required). Freezes the auto-receive deadline
/// (now + CargoDry.StockRequestAutoReceiveDays). Approved → Shipped.</summary>
public sealed class ShipProviderStockRequestCommand : AizenCommand<CargoDryStockRequestDto>
{
    public long   RequestId       { get; init; }
    public long   ShippedByUserId { get; init; }
    public string TrackingCode    { get; init; } = default!;
}

[DocumentationInfo("Ship provider stock request", "Marks an Approved stock request Shipped with a tracking code; freezes the auto-receive deadline.")]
public sealed class ShipProviderStockRequestCommandHandler
    : AizenCommandHandler<ShipProviderStockRequestCommand, CargoDryStockRequestDto>
{
    private readonly ICargoDryStockRequestRepository  _requests;
    private readonly IConfiguration                   _configuration;
    private readonly ICargoDryReferenceDataRemoteCall _referenceData;
    private readonly IAizenMessagePublisher           _messagePublisher;

    public ShipProviderStockRequestCommandHandler(
        ICargoDryStockRequestRepository requests, IConfiguration configuration,
        ICargoDryReferenceDataRemoteCall referenceData, IAizenMessagePublisher messagePublisher)
    {
        _requests      = requests;
        _configuration = configuration;
        _referenceData = referenceData;
        _messagePublisher = messagePublisher;
    }

    public override async Task<CargoDryStockRequestDto?> Handle(
        ShipProviderStockRequestCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TrackingCode))
            throw new AizenBusinessException("A tracking code is required to ship a stock request.");

        var entity = await _requests.GetByIdAsync(request.RequestId, ct)
            ?? throw new AizenBusinessException($"Stock request {request.RequestId} not found.");

        if (entity.Status != CargoDryStockRequestStatus.Approved)
            throw new AizenBusinessException($"Only an Approved stock request can be shipped (current: {entity.Status}).");

        var days = await CargoDryStockRequestOptions.ResolveAutoReceiveDaysAsync(_referenceData, _configuration, ct);
        var autoReceiveDeadline = DateTimeOffset.UtcNow.AddDays(days);

        entity.Ship(request.ShippedByUserId, request.TrackingCode, autoReceiveDeadline);
        await _requests.SaveChangesAsync(ct);

        await _messagePublisher.PublishAsync(new CargoDryStockRequestEventMessage
        {
            StockRequestId    = entity.Id,
            RequestCode       = entity.RequestCode,
            ProviderProfileId = entity.ProviderProfileId,
            ProductCode       = entity.ProductCode,
            Event             = CargoDryStockRequestEvent.Shipped,
            TrackingCode      = entity.TrackingCode,
        }, ct);

        return entity.ToDto();
    }
}
