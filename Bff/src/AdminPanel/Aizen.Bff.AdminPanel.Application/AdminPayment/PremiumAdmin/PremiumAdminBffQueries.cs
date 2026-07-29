using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.PremiumAdmin;

// ─── List products ───────────────────────────────────────────────────────────
public sealed class GetPremiumProductsBffQuery : AizenQuery<GetPremiumProductsBffResponse> { }
public sealed class GetPremiumProductsBffResponse { public List<PremiumProductAdminDto> Items { get; init; } = new(); }

[DocumentationInfo("Get premium products BFF query handler (P11)",
    "Lists all premium products (GET /admin/premium/products). Read-only.")]
public sealed class GetPremiumProductsBffQueryHandler
    : AizenQueryHandler<GetPremiumProductsBffQuery, GetPremiumProductsBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public GetPremiumProductsBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<GetPremiumProductsBffResponse?> Handle(GetPremiumProductsBffQuery request, CancellationToken ct)
        => new() { Items = await _payment.GetPremiumProductsAsync(ct) };
}

// ─── Get product by id ───────────────────────────────────────────────────────
public sealed class GetPremiumProductByIdBffQuery : AizenQuery<GetPremiumProductByIdBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetPremiumProductByIdBffResponse { public PremiumProductAdminDto? Result { get; init; } }

[DocumentationInfo("Get premium product by id BFF query handler (P11)",
    "Reads a single premium product (GET /admin/premium/products/{id}). Read-only.")]
public sealed class GetPremiumProductByIdBffQueryHandler
    : AizenQueryHandler<GetPremiumProductByIdBffQuery, GetPremiumProductByIdBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public GetPremiumProductByIdBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<GetPremiumProductByIdBffResponse?> Handle(GetPremiumProductByIdBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetPremiumProductByIdAsync(request.Id, ct) };
}

// ─── List prices for product ─────────────────────────────────────────────────
public sealed class GetPremiumProductPricesBffQuery : AizenQuery<GetPremiumProductPricesBffResponse>
{
    public long ProductId { get; init; }
}
public sealed class GetPremiumProductPricesBffResponse { public List<PremiumProductPriceAdminDto> Items { get; init; } = new(); }

[DocumentationInfo("Get premium product prices BFF query handler (P11)",
    "Lists all versioned prices for a premium product (GET /admin/premium/products/{productId}/prices). Read-only.")]
public sealed class GetPremiumProductPricesBffQueryHandler
    : AizenQueryHandler<GetPremiumProductPricesBffQuery, GetPremiumProductPricesBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public GetPremiumProductPricesBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<GetPremiumProductPricesBffResponse?> Handle(GetPremiumProductPricesBffQuery request, CancellationToken ct)
        => new() { Items = await _payment.GetPremiumProductPricesAsync(request.ProductId, ct) };
}

// ─── Resolve price (point-in-time) ───────────────────────────────────────────
public sealed class ResolvePremiumProductPriceBffQuery : AizenQuery<ResolvePremiumProductPriceBffResponse>
{
    public long      ProductId    { get; init; }
    public string    CurrencyCode { get; init; } = "TRY";
    public DateTime? AtUtc        { get; init; }
}
public sealed class ResolvePremiumProductPriceBffResponse { public PremiumProductPriceAdminDto? Result { get; init; } }

[DocumentationInfo("Resolve premium product price BFF query handler (P11)",
    "Point-in-time single-active premium price resolution (GET /admin/premium/products/{productId}/prices/resolve). Read-only.")]
public sealed class ResolvePremiumProductPriceBffQueryHandler
    : AizenQueryHandler<ResolvePremiumProductPriceBffQuery, ResolvePremiumProductPriceBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public ResolvePremiumProductPriceBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<ResolvePremiumProductPriceBffResponse?> Handle(ResolvePremiumProductPriceBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolvePremiumProductPriceAsync(request.ProductId, request.CurrencyCode, request.AtUtc, ct) };
}
