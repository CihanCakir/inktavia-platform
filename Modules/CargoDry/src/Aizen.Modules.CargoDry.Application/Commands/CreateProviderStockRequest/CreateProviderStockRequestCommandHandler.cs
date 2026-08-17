using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.CreateProviderStockRequest;

public sealed class CreateProviderStockRequestCommandHandler
    : AizenCommandHandler<CreateProviderStockRequestCommand, CargoDryStockRequestDto>
{
    private readonly ICargoDryStockRequestRepository _requests;
    private readonly ICargoDryProductRepository _products;

    public CreateProviderStockRequestCommandHandler(
        ICargoDryStockRequestRepository requests, ICargoDryProductRepository products)
    { _requests = requests; _products = products; }

    public override async Task<CargoDryStockRequestDto?> Handle(
        CreateProviderStockRequestCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ProductCode))
            throw new AizenBusinessException("Product code is required.");
        if (request.RequestedQuantity < 1 || request.RequestedQuantity > 1000)
            throw new AizenBusinessException("Requested quantity must be between 1 and 1000.");

        var product = await _products.GetByCodeAsync(request.ProductCode, ct)
            ?? throw new AizenBusinessException("Product not found.");

        if (await _requests.HasPendingForProductAsync(request.ProviderProfileId, request.ProductCode, ct))
            throw new AizenBusinessException("SR_STOCK_REQUEST_DUPLICATE_PENDING");

        var entity = CargoDryStockRequestEntity.Create(
            request.ProviderProfileId, request.ProductCode, request.RequestedQuantity,
            null, request.Note);

        await _requests.AddAsync(entity, ct);
        await _requests.SaveChangesAsync(ct);

        return new CargoDryStockRequestDto
        {
            Id = entity.Id, RequestCode = entity.RequestCode,
            ProviderProfileId = entity.ProviderProfileId, ProductCode = entity.ProductCode,
            RequestedQuantity = entity.RequestedQuantity,
            Status = (int)entity.Status, StatusName = entity.Status.ToString(),
            ProviderNote = entity.ProviderNote,
            CreatedAtUtc = entity.CreateDate.HasValue ? new DateTimeOffset(entity.CreateDate.Value, TimeSpan.Zero) : DateTimeOffset.UtcNow
        };
    }
}
