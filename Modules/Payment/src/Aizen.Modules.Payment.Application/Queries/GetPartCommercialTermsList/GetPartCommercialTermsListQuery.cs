using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPartCommercialTermsList;

/// <summary>BE-S5a — admin list of part commercial terms (optional scope/currency/active filters). Admin-only; carries cost.</summary>
public sealed class GetPartCommercialTermsListQuery : AizenQuery<PartCommercialTermListResult>
{
    public string? Brand             { get; init; }
    public string? ProductCode       { get; init; }
    public long?   ProviderProfileId { get; init; }
    public string? CategoryCode      { get; init; }
    public string? CurrencyCode      { get; init; }
    public bool?   IsActive          { get; init; }
}

[DocumentationInfo("GetPartCommercialTermsListQueryHandler",
    "Returns the part commercial term list (no paging — terms are few). Optional scope/currency/active filters, applied " +
    "in-memory. Sorted by currency then EffectiveFrom desc. Admin-only surface (carries cost).")]
public sealed class GetPartCommercialTermsListQueryHandler
    : AizenQueryHandler<GetPartCommercialTermsListQuery, PartCommercialTermListResult>
{
    private readonly IPartCommercialTermRepository _terms;
    public GetPartCommercialTermsListQueryHandler(IPartCommercialTermRepository terms) => _terms = terms;

    public override async Task<PartCommercialTermListResult?> Handle(
        GetPartCommercialTermsListQuery request, CancellationToken ct)
    {
        var all = await _terms.GetAllAsync(ct);
        IEnumerable<Domain.Entities.PartCommercialTerm.PartCommercialTermEntity> filtered = all;

        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            var v = request.Brand.ToUpperInvariant();
            filtered = filtered.Where(t => t.Brand == v);
        }
        if (!string.IsNullOrWhiteSpace(request.ProductCode))
        {
            var v = request.ProductCode.ToUpperInvariant();
            filtered = filtered.Where(t => t.ProductCode == v);
        }
        if (request.ProviderProfileId.HasValue)
            filtered = filtered.Where(t => t.ProviderProfileId == request.ProviderProfileId.Value);
        if (!string.IsNullOrWhiteSpace(request.CategoryCode))
        {
            var v = request.CategoryCode.ToUpperInvariant();
            filtered = filtered.Where(t => t.CategoryCode == v);
        }
        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            var v = request.CurrencyCode.ToUpperInvariant();
            filtered = filtered.Where(t => t.CurrencyCode == v);
        }
        if (request.IsActive.HasValue)
            filtered = filtered.Where(t => t.IsActive == request.IsActive.Value);

        var items = filtered
            .OrderBy(t => t.CurrencyCode)
            .ThenByDescending(t => t.EffectiveFrom)
            .Select(PartCommercialTermDtoMapper.ToDto)
            .ToList();

        return new PartCommercialTermListResult(items, items.Count);
    }
}
