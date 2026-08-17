using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Application.Queries.PremiumAdmin;

// ─── Products list ───────────────────────────────────────────────────────────
public sealed class GetPremiumProductsQuery : AizenQuery<List<PremiumProductAdminDto>> { }

public sealed class GetPremiumProductsQueryHandler : AizenQueryHandler<GetPremiumProductsQuery, List<PremiumProductAdminDto>>
{
    private readonly PaymentDbContext _db;
    public GetPremiumProductsQueryHandler(PaymentDbContext db) => _db = db;

    public override async Task<List<PremiumProductAdminDto>?> Handle(GetPremiumProductsQuery request, CancellationToken ct)
        => await _db.PremiumProducts.AsNoTracking().OrderBy(p => p.Code)
            .Select(p => Map(p)).ToListAsync(ct);

    internal static PremiumProductAdminDto Map(PremiumProductEntity p) => new()
    {
        Id = p.Id, Code = p.Code, Name = p.Name, EntitlementType = (int)p.EntitlementType,
        DurationDays = p.DurationDays, Status = (int)p.Status, Description = p.Description, IsActive = p.IsActive,
    };
}

// ─── Product by id ───────────────────────────────────────────────────────────
public sealed class GetPremiumProductByIdQuery : AizenQuery<PremiumProductAdminDto> { public long Id { get; init; } }

public sealed class GetPremiumProductByIdQueryHandler : AizenQueryHandler<GetPremiumProductByIdQuery, PremiumProductAdminDto>
{
    private readonly IPremiumProductRepository _products;
    public GetPremiumProductByIdQueryHandler(IPremiumProductRepository products) => _products = products;

    public override async Task<PremiumProductAdminDto?> Handle(GetPremiumProductByIdQuery request, CancellationToken ct)
    {
        var p = await _products.GetByIdAsync(request.Id, ct);
        return p is null ? null : GetPremiumProductsQueryHandler.Map(p);
    }
}

// ─── Prices for a product ────────────────────────────────────────────────────
public sealed class GetPremiumProductPricesQuery : AizenQuery<List<PremiumProductPriceAdminDto>> { public long PremiumProductId { get; init; } }

public sealed class GetPremiumProductPricesQueryHandler : AizenQueryHandler<GetPremiumProductPricesQuery, List<PremiumProductPriceAdminDto>>
{
    private readonly IPremiumProductPriceRepository _prices;
    public GetPremiumProductPricesQueryHandler(IPremiumProductPriceRepository prices) => _prices = prices;

    public override async Task<List<PremiumProductPriceAdminDto>?> Handle(GetPremiumProductPricesQuery request, CancellationToken ct)
    {
        var list = await _prices.GetByProductAsync(request.PremiumProductId, ct);
        return list.Select(MapPrice).ToList();
    }

    internal static PremiumProductPriceAdminDto MapPrice(PremiumProductPriceEntity x) => new()
    {
        Id = x.Id, PremiumProductId = x.PremiumProductId, PriceAmount = x.PriceAmount, CurrencyCode = x.CurrencyCode,
        EffectiveFrom = x.EffectiveFrom, EffectiveTo = x.EffectiveTo, Status = (int)x.Status,
        PriceCode = x.PriceCode, Notes = x.Notes, IsActive = x.IsActive,
    };
}

// ─── Resolve point-in-time price ─────────────────────────────────────────────
public sealed class ResolvePremiumProductPriceQuery : AizenQuery<PremiumProductPriceAdminDto>
{
    public long      PremiumProductId { get; init; }
    public string    CurrencyCode     { get; init; } = "TRY";
    public DateTime? AtUtc            { get; init; }
}

public sealed class ResolvePremiumProductPriceQueryHandler : AizenQueryHandler<ResolvePremiumProductPriceQuery, PremiumProductPriceAdminDto>
{
    private readonly IPremiumProductPriceRepository _prices;
    public ResolvePremiumProductPriceQueryHandler(IPremiumProductPriceRepository prices) => _prices = prices;

    public override async Task<PremiumProductPriceAdminDto?> Handle(ResolvePremiumProductPriceQuery request, CancellationToken ct)
    {
        var p = await _prices.ResolveAsync(request.PremiumProductId, request.CurrencyCode, request.AtUtc ?? DateTime.UtcNow, ct);
        return p is null ? null : GetPremiumProductPricesQueryHandler.MapPrice(p);
    }
}
