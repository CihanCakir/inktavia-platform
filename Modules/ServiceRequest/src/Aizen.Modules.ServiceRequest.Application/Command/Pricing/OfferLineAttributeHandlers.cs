using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Services.Pricing;
using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Command.Pricing;

public sealed class SetOfferLineAttributesCommandHandler
    : AizenCommandHandler<SetOfferLineAttributesCommand, List<PricingAttributeValueDto>>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;
    private readonly PricingAttributeValidator _validator;

    public SetOfferLineAttributesCommandHandler(ServiceRequestDbContext db, IAizenInfoAccessor info, PricingAttributeValidator validator)
    { _db = db; _info = info; _validator = validator; }

    public override async Task<List<PricingAttributeValueDto>?> Handle(SetOfferLineAttributesCommand command, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        var offer = await _db.ServiceRequestOffers
            .FirstOrDefaultAsync(o => o.Id == command.OfferId && o.ProviderProfileId == profileId && !o.IsDeleted, ct)
            ?? throw new AizenBusinessException("SR_PRICING_ATTR_OFFER_NOT_FOUND");

        if (offer.Status is ServiceRequestOfferStatus.Accepted or ServiceRequestOfferStatus.Rejected
            or ServiceRequestOfferStatus.Withdrawn or ServiceRequestOfferStatus.Expired)
            throw new AizenBusinessException($"SR_PRICING_ATTR_OFFER_LOCKED: offer is {offer.Status}");

        var item = await _db.ServiceRequestOfferItems
            .FirstOrDefaultAsync(i => i.Id == command.OfferItemId && i.ServiceRequestOfferId == command.OfferId && !i.IsDeleted, ct)
            ?? throw new AizenBusinessException("SR_PRICING_ATTR_LINE_NOT_FOUND");

        var sr = await _db.ServiceRequests.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == offer.ServiceRequestId, ct)
            ?? throw new AizenBusinessException("SR_PRICING_ATTR_SR_NOT_FOUND");

        var attrs = command.Request?.Attributes ?? new();
        await _validator.ValidateAsync(sr.ServiceCategoryCode, attrs, ct);

        // Full replace for this line.
        var existing = await _db.PricingAttributeValues.Where(v => v.OfferItemId == item.Id).ToListAsync(ct);
        if (existing.Count > 0) _db.PricingAttributeValues.RemoveRange(existing);

        var created = attrs.Select(a => PricingAttributeValueEntity.Create(
            item.Id, a.DefinitionCode, a.ValueLookupItemCode, a.ValueNumber, a.ValueText, a.ValueBool)).ToList();
        if (created.Count > 0) _db.PricingAttributeValues.AddRange(created);

        await _db.SaveChangesAsync(ct);
        return created.Select(ToDto).ToList();
    }

    internal static PricingAttributeValueDto ToDto(PricingAttributeValueEntity e) => new()
    {
        OfferItemId = e.OfferItemId, DefinitionCode = e.DefinitionCode,
        ValueLookupItemCode = e.ValueLookupItemCode, ValueNumber = e.ValueNumber,
        ValueText = e.ValueText, ValueBool = e.ValueBool,
    };
}

public sealed class GetOfferLineAttributesQueryHandler
    : AizenQueryHandler<GetOfferLineAttributesQuery, List<PricingAttributeValueDto>>
{
    private readonly ServiceRequestDbContext _db;
    public GetOfferLineAttributesQueryHandler(ServiceRequestDbContext db) => _db = db;

    public override async Task<List<PricingAttributeValueDto>?> Handle(GetOfferLineAttributesQuery query, CancellationToken ct)
    {
        return await _db.PricingAttributeValues.AsNoTracking()
            .Where(v => v.OfferItemId == query.OfferItemId)
            .OrderBy(v => v.DefinitionCode)
            .Select(v => new PricingAttributeValueDto
            {
                OfferItemId = v.OfferItemId, DefinitionCode = v.DefinitionCode,
                ValueLookupItemCode = v.ValueLookupItemCode, ValueNumber = v.ValueNumber,
                ValueText = v.ValueText, ValueBool = v.ValueBool,
            })
            .ToListAsync(ct);
    }
}

public sealed class GetApplicablePricingAttributesQueryHandler
    : AizenQueryHandler<GetApplicablePricingAttributesQuery, List<ApplicablePricingAttributeDto>>
{
    private readonly ServiceRequestDbContext _db;
    private readonly PricingAttributeValidator _validator;
    private readonly ReferenceDataLookupClient _lookup;

    public GetApplicablePricingAttributesQueryHandler(
        ServiceRequestDbContext db, PricingAttributeValidator validator, ReferenceDataLookupClient lookup)
    { _db = db; _validator = validator; _lookup = lookup; }

    public override async Task<List<ApplicablePricingAttributeDto>?> Handle(GetApplicablePricingAttributesQuery query, CancellationToken ct)
    {
        var sr = await _db.ServiceRequests.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.ServiceRequestId, ct)
            ?? throw new AizenBusinessException("SR_PRICING_ATTR_SR_NOT_FOUND");

        var defs = await _validator.GetApplicableDefinitionsAsync(sr.ServiceCategoryCode, ct);
        var result = new List<ApplicablePricingAttributeDto>(defs.Count);
        foreach (var d in defs)
        {
            var dto = new ApplicablePricingAttributeDto
            {
                Code = d.Code, NameTr = d.NameTr, NameEn = d.NameEn, DataType = d.DataType,
                LookupGroupCode = d.LookupGroupCode, IsRequired = d.IsRequired, SortOrder = d.SortOrder,
                MinValue = d.MinValue, MaxValue = d.MaxValue,
            };
            if (d.DataType == PricingAttributeDataType.Lookup && !string.IsNullOrWhiteSpace(d.LookupGroupCode))
            {
                var items = await _lookup.GetActiveItemsAsync(d.LookupGroupCode, ct);
                dto.Options = items.Select(i => new PricingAttributeOptionDto
                { Code = i.Code, NameTr = i.Name, NameEn = i.Description }).ToList();
            }
            result.Add(dto);
        }
        return result;
    }
}
