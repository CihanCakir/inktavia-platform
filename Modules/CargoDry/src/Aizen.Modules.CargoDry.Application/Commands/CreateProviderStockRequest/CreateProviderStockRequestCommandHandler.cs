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
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public CreateProviderStockRequestCommandHandler(
        ICargoDryStockRequestRepository requests,
        ICargoDryProductRepository products,
        ICargoDryConsignmentAgreementRepository agreements)
    { _requests = requests; _products = products; _agreements = agreements; }


    /// <summary>
    /// PROV-MVP-002 — participation is checked HERE, not only at the BFF.
    ///
    /// Verified 2026-08-24: neither this handler nor the BFF checked whether the caller is in the CargoDry
    /// programme. The BFF's only gate was `ProviderActive` ("any approved, active provider on the platform") and
    /// this handler validated product code, quantity, product existence and duplicate-pending — all questions
    /// about the REQUEST, none about the CALLER. A provider whose onboarding said
    /// `CargoDryInterest.interested = false` could create a real stock-request row against any product.
    ///
    /// A BFF policy alone is not sufficient: `CargoDryProviderController` carries a bare [Authorize] and resolves
    /// the provider from the `provider_profile_id` token CLAIM, so the module is reachable by anything holding a
    /// valid token — not only by the BFF. The write model has to defend itself.
    /// </summary>
    public override async Task<CargoDryStockRequestDto?> Handle(
        CreateProviderStockRequestCommand request, CancellationToken ct)
    {
        var activeAgreements = await _agreements.CountActiveForProviderAsync(
            request.ProviderProfileId, DateTime.UtcNow, ct);
        if (activeAgreements == 0)
            throw new AizenBusinessException("CD_PROVIDER_NOT_IN_PROGRAMME");

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
