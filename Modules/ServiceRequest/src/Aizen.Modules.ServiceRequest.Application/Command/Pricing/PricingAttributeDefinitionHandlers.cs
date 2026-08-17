using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Application.Services.Pricing;
using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Command.Pricing;

public sealed class CreatePricingAttributeDefinitionCommandHandler
    : AizenCommandHandler<CreatePricingAttributeDefinitionCommand, PricingAttributeDefinitionDto>
{
    private readonly ServiceRequestDbContext _db;
    private readonly PricingAttributeDefinitionValidator _validator;

    public CreatePricingAttributeDefinitionCommandHandler(ServiceRequestDbContext db, PricingAttributeDefinitionValidator validator)
    { _db = db; _validator = validator; }

    public override async Task<PricingAttributeDefinitionDto?> Handle(CreatePricingAttributeDefinitionCommand command, CancellationToken ct)
    {
        var req = command.Request;
        await _validator.ValidateAsync(req, ct);

        var code = req.Code.Trim().ToUpperInvariant();
        if (await _db.PricingAttributeDefinitions.AnyAsync(d => d.Code == code, ct))
            throw new AizenBusinessException($"SR_PRICING_ATTR_CODE_EXISTS: '{code}'");

        var entity = PricingAttributeDefinitionEntity.Create(
            req.Code, req.NameTr, req.NameEn, req.DataType, req.LookupGroupCode,
            req.IsRequired, req.SortOrder, req.MinValue, req.MaxValue, req.ServiceCategoryCodes);

        _db.PricingAttributeDefinitions.Add(entity);
        await _db.SaveChangesAsync(ct);
        return PricingAttributeDefinitionMapping.ToDto(entity);
    }
}

public sealed class UpdatePricingAttributeDefinitionCommandHandler
    : AizenCommandHandler<UpdatePricingAttributeDefinitionCommand, PricingAttributeDefinitionDto>
{
    private readonly ServiceRequestDbContext _db;
    private readonly PricingAttributeDefinitionValidator _validator;

    public UpdatePricingAttributeDefinitionCommandHandler(ServiceRequestDbContext db, PricingAttributeDefinitionValidator validator)
    { _db = db; _validator = validator; }

    public override async Task<PricingAttributeDefinitionDto?> Handle(UpdatePricingAttributeDefinitionCommand command, CancellationToken ct)
    {
        var req = command.Request;
        await _validator.ValidateAsync(req, ct);

        var entity = await _db.PricingAttributeDefinitions
            .Include(d => d.Categories)
            .FirstOrDefaultAsync(d => d.Id == command.Id, ct)
            ?? throw new AizenBusinessException("SR_PRICING_ATTR_NOT_FOUND");

        entity.Update(
            req.NameTr, req.NameEn, req.DataType, req.LookupGroupCode,
            req.IsRequired, req.SortOrder, req.MinValue, req.MaxValue, req.ServiceCategoryCodes);

        await _db.SaveChangesAsync(ct);
        return PricingAttributeDefinitionMapping.ToDto(entity);
    }
}

public sealed class DeletePricingAttributeDefinitionCommandHandler
    : AizenCommandHandler<DeletePricingAttributeDefinitionCommand, bool>
{
    private readonly ServiceRequestDbContext _db;
    public DeletePricingAttributeDefinitionCommandHandler(ServiceRequestDbContext db) => _db = db;

    public override async Task<bool> Handle(DeletePricingAttributeDefinitionCommand command, CancellationToken ct)
    {
        var entity = await _db.PricingAttributeDefinitions.FirstOrDefaultAsync(d => d.Id == command.Id, ct)
            ?? throw new AizenBusinessException("SR_PRICING_ATTR_NOT_FOUND");

        // Deactivate (keep the row + its stable Code so it can be reactivated and existing snapshots stay resolvable).
        entity.Deactivate();
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class ListPricingAttributeDefinitionsQueryHandler
    : AizenQueryHandler<ListPricingAttributeDefinitionsQuery, List<PricingAttributeDefinitionDto>>
{
    private readonly ServiceRequestDbContext _db;
    public ListPricingAttributeDefinitionsQueryHandler(ServiceRequestDbContext db) => _db = db;

    public override async Task<List<PricingAttributeDefinitionDto>?> Handle(ListPricingAttributeDefinitionsQuery query, CancellationToken ct)
    {
        var q = _db.PricingAttributeDefinitions.AsNoTracking().Include(d => d.Categories).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.ServiceCategoryCode))
        {
            var cat = query.ServiceCategoryCode.Trim().ToUpperInvariant();
            q = q.Where(d => d.Categories.Any(c => c.ServiceCategoryCode == cat));
        }

        var list = await q.OrderBy(d => d.SortOrder).ThenBy(d => d.Code).ToListAsync(ct);
        return list.Select(PricingAttributeDefinitionMapping.ToDto).ToList();
    }
}

internal static class PricingAttributeDefinitionMapping
{
    public static PricingAttributeDefinitionDto ToDto(PricingAttributeDefinitionEntity e) => new()
    {
        Id = e.Id, Code = e.Code, NameTr = e.NameTr, NameEn = e.NameEn, DataType = e.DataType,
        LookupGroupCode = e.LookupGroupCode, IsRequired = e.IsRequired, SortOrder = e.SortOrder,
        MinValue = e.MinValue, MaxValue = e.MaxValue, IsActive = e.IsActive,
        ServiceCategoryCodes = e.Categories.Select(c => c.ServiceCategoryCode).OrderBy(c => c).ToList(),
    };
}
