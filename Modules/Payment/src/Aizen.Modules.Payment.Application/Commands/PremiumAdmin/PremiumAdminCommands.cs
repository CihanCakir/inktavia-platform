using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Commands.PremiumAdmin;

// ═══ PremiumProduct CRUD ═════════════════════════════════════════════════════

public sealed class CreatePremiumProductCommand : AizenCommand<PremiumMutateResultDto>
{
    public string                 Code            { get; init; } = default!;
    public string                 Name            { get; init; } = default!;
    public PremiumEntitlementType EntitlementType { get; init; } = PremiumEntitlementType.OfferBoost;
    public int                    DurationDays    { get; init; }
    public string?                Description     { get; init; }
}

public sealed class CreatePremiumProductCommandHandler : AizenCommandHandler<CreatePremiumProductCommand, PremiumMutateResultDto>
{
    private readonly IPremiumProductRepository _products;
    public CreatePremiumProductCommandHandler(IPremiumProductRepository products) => _products = products;

    public override async Task<PremiumMutateResultDto?> Handle(CreatePremiumProductCommand r, CancellationToken ct)
    {
        var existing = await _products.GetByCodeAsync(r.Code, ct);
        if (existing is not null)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductPriceConflict, $"A premium product with code {r.Code} already exists.");

        var product = PremiumProductEntity.Create(r.Code, r.Name, r.EntitlementType, r.DurationDays, PremiumProductStatus.Active, r.Description);
        await _products.AddAsync(product, ct);
        await _products.SaveChangesAsync(ct);   // materialise Id
        return new PremiumMutateResultDto(product.Id, product.Code);
    }
}

public sealed class UpdatePremiumProductCommand : AizenCommand<PremiumMutateResultDto>
{
    public long    Id           { get; init; }
    public string  Name         { get; init; } = default!;
    public int     DurationDays { get; init; }
    public string? Description   { get; init; }
}

public sealed class UpdatePremiumProductCommandHandler : AizenCommandHandler<UpdatePremiumProductCommand, PremiumMutateResultDto>
{
    private readonly IPremiumProductRepository _products;
    public UpdatePremiumProductCommandHandler(IPremiumProductRepository products) => _products = products;

    public override async Task<PremiumMutateResultDto?> Handle(UpdatePremiumProductCommand r, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(r.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductNotFound, $"Premium product {r.Id} not found.");
        product.Update(r.Name, r.DurationDays, r.Description);
        _products.Update(product);
        return new PremiumMutateResultDto(product.Id, product.Code);
    }
}

/// <summary>Activate or deactivate a premium product (Activate=true → Active; false → Inactive).</summary>
public sealed class SetPremiumProductStatusCommand : AizenCommand<PremiumMutateResultDto>
{
    public long Id       { get; init; }
    public bool Activate { get; init; }
}

public sealed class SetPremiumProductStatusCommandHandler : AizenCommandHandler<SetPremiumProductStatusCommand, PremiumMutateResultDto>
{
    private readonly IPremiumProductRepository _products;
    public SetPremiumProductStatusCommandHandler(IPremiumProductRepository products) => _products = products;

    public override async Task<PremiumMutateResultDto?> Handle(SetPremiumProductStatusCommand r, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(r.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductNotFound, $"Premium product {r.Id} not found.");
        if (r.Activate) product.Activate(); else product.Deactivate();
        _products.Update(product);
        return new PremiumMutateResultDto(product.Id, product.Code);
    }
}

// ═══ PremiumProductPrice CRUD (point-in-time; overlap → PremiumProductPriceConflict) ═══

public sealed class CreatePremiumProductPriceCommand : AizenCommand<PremiumMutateResultDto>
{
    public long      PremiumProductId { get; init; }
    public decimal   PriceAmount      { get; init; }
    public string    CurrencyCode     { get; init; } = "TRY";
    public DateTime  EffectiveFrom    { get; init; }
    public DateTime? EffectiveTo      { get; init; }
    public string?   Notes            { get; init; }
}

public sealed class CreatePremiumProductPriceCommandHandler : AizenCommandHandler<CreatePremiumProductPriceCommand, PremiumMutateResultDto>
{
    private readonly IPremiumProductPriceRepository _prices;
    public CreatePremiumProductPriceCommandHandler(IPremiumProductPriceRepository prices) => _prices = prices;

    public override async Task<PremiumMutateResultDto?> Handle(CreatePremiumProductPriceCommand r, CancellationToken ct)
    {
        var price = PremiumProductPriceEntity.Create(
            r.PremiumProductId, r.PriceAmount, r.CurrencyCode,
            r.EffectiveFrom.ToUniversalTime(), r.EffectiveTo?.ToUniversalTime(), priceCode: null, notes: r.Notes);

        var guard = await _prices.ValidateInsertableAsync(price, ct);
        if (guard.Outcome == PremiumPriceGuardOutcome.Overlap)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductPriceConflict,
                $"The price range overlaps an existing active premium price (Id={guard.ConflictingId}).");

        await _prices.AddAsync(price, ct);
        await _prices.SaveChangesAsync(ct);
        return new PremiumMutateResultDto(price.Id, price.PriceCode);
    }
}

public sealed class UpdatePremiumProductPriceCommand : AizenCommand<PremiumMutateResultDto>
{
    public long      Id            { get; init; }
    public decimal   PriceAmount   { get; init; }
    public DateTime  EffectiveFrom { get; init; }
    public DateTime? EffectiveTo   { get; init; }
    public string?   Notes         { get; init; }
}

public sealed class UpdatePremiumProductPriceCommandHandler : AizenCommandHandler<UpdatePremiumProductPriceCommand, PremiumMutateResultDto>
{
    private readonly IPremiumProductPriceRepository _prices;
    public UpdatePremiumProductPriceCommandHandler(IPremiumProductPriceRepository prices) => _prices = prices;

    public override async Task<PremiumMutateResultDto?> Handle(UpdatePremiumProductPriceCommand r, CancellationToken ct)
    {
        var price = await _prices.GetByIdAsync(r.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductPriceNotFound, $"Premium price {r.Id} not found.");
        price.Update(r.PriceAmount, r.EffectiveFrom.ToUniversalTime(), r.EffectiveTo?.ToUniversalTime(), r.Notes);

        var guard = await _prices.ValidateInsertableAsync(price, ct);
        if (guard.Outcome == PremiumPriceGuardOutcome.Overlap)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductPriceConflict,
                $"The price range overlaps an existing active premium price (Id={guard.ConflictingId}).");

        _prices.Update(price);
        return new PremiumMutateResultDto(price.Id, price.PriceCode);
    }
}

public sealed class DeactivatePremiumProductPriceCommand : AizenCommand<PremiumMutateResultDto>
{
    public long Id { get; init; }
}

public sealed class DeactivatePremiumProductPriceCommandHandler : AizenCommandHandler<DeactivatePremiumProductPriceCommand, PremiumMutateResultDto>
{
    private readonly IPremiumProductPriceRepository _prices;
    public DeactivatePremiumProductPriceCommandHandler(IPremiumProductPriceRepository prices) => _prices = prices;

    public override async Task<PremiumMutateResultDto?> Handle(DeactivatePremiumProductPriceCommand r, CancellationToken ct)
    {
        var price = await _prices.GetByIdAsync(r.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductPriceNotFound, $"Premium price {r.Id} not found.");
        price.Deactivate();
        _prices.Update(price);
        return new PremiumMutateResultDto(price.Id, price.PriceCode);
    }
}
