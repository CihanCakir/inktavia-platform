using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Services.Pricing;

/// <summary>
/// S2d — at acceptance, gathers each offer line's <c>PricingAttributeValue</c> rows, resolves the (denormalized) display
/// label for Lookup values via the R4 remote call, and projects them to the acceptance-economics request DTOs keyed by
/// LineRef (= offer item id). Payment writes them as immutable <c>OfferLineAttributeSnapshot</c> children of the line
/// economics snapshot. Values were already validated at offer-build time (S2b); this path does <b>no re-valuation</b>.
/// </summary>
public sealed class PricingAttributeSnapshotResolver
{
    private readonly ServiceRequestDbContext _db;
    private readonly ReferenceDataLookupClient _lookup;

    public PricingAttributeSnapshotResolver(ServiceRequestDbContext db, ReferenceDataLookupClient lookup)
    { _db = db; _lookup = lookup; }

    public async Task<IReadOnlyDictionary<string, List<CalculateServiceRequestEconomicsAttributeDto>>> ResolveForOfferAsync(
        ServiceRequestOfferEntity offer, CancellationToken ct)
    {
        var empty = new Dictionary<string, List<CalculateServiceRequestEconomicsAttributeDto>>();

        var itemIds = offer.Items.Select(i => i.Id).ToList();
        if (itemIds.Count == 0) return empty;

        var values = await _db.PricingAttributeValues.AsNoTracking()
            .Where(v => itemIds.Contains(v.OfferItemId))
            .ToListAsync(ct);
        if (values.Count == 0) return empty;

        var codes = values.Select(v => v.DefinitionCode).Distinct().ToList();
        var defs = await _db.PricingAttributeDefinitions.AsNoTracking()
            .Where(d => codes.Contains(d.Code))
            .ToListAsync(ct);
        var defByCode = defs.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, List<CalculateServiceRequestEconomicsAttributeDto>>();
        foreach (var group in values.GroupBy(v => v.OfferItemId))
        {
            var lineRef = group.Key.ToString();
            var list = new List<CalculateServiceRequestEconomicsAttributeDto>();
            foreach (var v in group.OrderBy(x => x.DefinitionCode))
            {
                defByCode.TryGetValue(v.DefinitionCode, out var def);
                var dataType = def?.DataType ?? InferDataType(v);

                string? label = null;
                if (!string.IsNullOrWhiteSpace(v.ValueLookupItemCode) && !string.IsNullOrWhiteSpace(def?.LookupGroupCode))
                    label = await _lookup.ResolveItemLabelAsync(def!.LookupGroupCode!, v.ValueLookupItemCode!, ct);

                list.Add(new CalculateServiceRequestEconomicsAttributeDto
                {
                    DefinitionCode       = v.DefinitionCode,
                    DataType             = (int)dataType,
                    ValueLookupItemCode  = v.ValueLookupItemCode,
                    ValueLookupItemLabel = label,
                    ValueNumber          = v.ValueNumber,
                    ValueText            = v.ValueText,
                    ValueBool            = v.ValueBool,
                    SortOrder            = def?.SortOrder ?? 0,
                });
            }
            result[lineRef] = list;
        }
        return result;
    }

    private static PricingAttributeDataType InferDataType(Domain.Entities.Pricing.PricingAttributeValueEntity v)
        => !string.IsNullOrWhiteSpace(v.ValueLookupItemCode) ? PricingAttributeDataType.Lookup
         : v.ValueNumber.HasValue ? PricingAttributeDataType.Number
         : v.ValueBool.HasValue ? PricingAttributeDataType.Boolean
         : PricingAttributeDataType.Text;
}
