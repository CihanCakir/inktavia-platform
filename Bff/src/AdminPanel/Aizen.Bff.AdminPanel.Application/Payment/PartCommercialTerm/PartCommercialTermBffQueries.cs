using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.PartCommercialTerm;

// ─── PartCommercialTerm: List (no paging) ────────────────────────────────────
public sealed class GetPartCommercialTermsListBffQuery : AizenQuery<GetPartCommercialTermsListBffResponse>
{
    public string? Brand             { get; init; }
    public string? ProductCode       { get; init; }
    public long?   ProviderProfileId { get; init; }
    public string? CategoryCode      { get; init; }
    public string? CurrencyCode      { get; init; }
    public bool?   IsActive          { get; init; }
}
public sealed class GetPartCommercialTermsListBffResponse { public PartCommercialTermListBffResult Result { get; init; } = default!; }

[DocumentationInfo("Get part commercial terms list BFF query handler (BE-S5)",
    "Returns the part commercial term list (no paging) with optional brand / productCode / provider / category / currency / " +
    "active filters, forwarded to the Payment module. Admin-only surface — carries confidential cost. Read-only.")]
public sealed class GetPartCommercialTermsListBffQueryHandler
    : AizenQueryHandler<GetPartCommercialTermsListBffQuery, GetPartCommercialTermsListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPartCommercialTermsListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPartCommercialTermsListBffResponse?> Handle(GetPartCommercialTermsListBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ListPartCommercialTermsAsync(
            request.Brand, request.ProductCode, request.ProviderProfileId, request.CategoryCode, request.CurrencyCode, request.IsActive, ct) };
}

// ─── PartCommercialTerm: Detail (by id) ──────────────────────────────────────
public sealed class GetPartCommercialTermDetailBffQuery : AizenQuery<GetPartCommercialTermDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetPartCommercialTermDetailBffResponse { public PartCommercialTermBffDto? Term { get; init; } }

[DocumentationInfo("Get part commercial term detail BFF query handler (BE-S5)",
    "Fetches a single part commercial term by ID from the Payment module (GET /part-commercial-term/rules/{id}). " +
    "A PartCommercialTermNotFound surfaces through the envelope. Admin-only. Read-only.")]
public sealed class GetPartCommercialTermDetailBffQueryHandler
    : AizenQueryHandler<GetPartCommercialTermDetailBffQuery, GetPartCommercialTermDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPartCommercialTermDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPartCommercialTermDetailBffResponse?> Handle(GetPartCommercialTermDetailBffQuery request, CancellationToken ct)
        => new() { Term = await _payment.GetPartCommercialTermDetailAsync(request.Id, ct) };
}
