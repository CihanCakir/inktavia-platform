using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Entities.Catalog;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Command.Catalog;

public sealed class CreateTemplateCommandHandler : AizenCommandHandler<CreateTemplateCommand, ProviderOfferTemplateDto>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;
    private readonly UnitCodeValidator _unitValidator;

    public CreateTemplateCommandHandler(ServiceRequestDbContext db, IAizenInfoAccessor info, UnitCodeValidator unitValidator)
    { _db = db; _info = info; _unitValidator = unitValidator; }

    public override async Task<ProviderOfferTemplateDto?> Handle(CreateTemplateCommand command, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AizenBusinessException("Template name is required.");
        if (req.Items.Count == 0) throw new AizenBusinessException("Template must have at least one item.");

        var unitItems = req.Items.Where(i => !string.IsNullOrWhiteSpace(i.UnitCode))
            .Select(i => new CreateServiceRequestOfferItemRequest { UnitCode = i.UnitCode, Title = i.Title }).ToList();
        if (unitItems.Count > 0) await _unitValidator.ValidateUnitCodesAsync(unitItems, ct);

        var template = ProviderOfferTemplateEntity.Create(profileId, req.Name, req.Description);
        var items = req.Items.Select((i, idx) => ProviderOfferTemplateItemEntity.Create(
            i.ItemType, i.Title, i.Description, i.Quantity, i.UnitCode, i.UnitPrice, i.CurrencyCode,
            i.TaxRate, i.DiscountType, i.DiscountValue, i.SortOrder > 0 ? i.SortOrder : idx));
        template.ReplaceItems(items);

        _db.ProviderOfferTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        return ToDto(template);
    }

    private static ProviderOfferTemplateDto ToDto(ProviderOfferTemplateEntity t) => new()
    {
        Id = t.Id, Name = t.Name, Description = t.Description,
        Items = t.Items.Select(i => new ProviderOfferTemplateItemDto
        {
            Id = i.Id, ItemType = i.ItemType, Title = i.Title, Description = i.Description,
            Quantity = i.Quantity, UnitCode = i.UnitCode, UnitPrice = i.UnitPrice, CurrencyCode = i.CurrencyCode,
            TaxRate = i.TaxRate, DiscountType = i.DiscountType, DiscountValue = i.DiscountValue, SortOrder = i.SortOrder
        }).ToList()
    };
}

public sealed class UpdateTemplateCommandHandler : AizenCommandHandler<UpdateTemplateCommand, ProviderOfferTemplateDto>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;
    private readonly UnitCodeValidator _unitValidator;

    public UpdateTemplateCommandHandler(ServiceRequestDbContext db, IAizenInfoAccessor info, UnitCodeValidator unitValidator)
    { _db = db; _info = info; _unitValidator = unitValidator; }

    public override async Task<ProviderOfferTemplateDto?> Handle(UpdateTemplateCommand command, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        var template = await _db.ProviderOfferTemplates.Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == command.TemplateId && t.ProviderProfileId == profileId && !t.IsDeleted, ct)
            ?? throw new AizenBusinessException("Template not found.");

        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AizenBusinessException("Template name is required.");
        if (req.Items.Count == 0) throw new AizenBusinessException("Template must have at least one item.");

        var unitItems = req.Items.Where(i => !string.IsNullOrWhiteSpace(i.UnitCode))
            .Select(i => new CreateServiceRequestOfferItemRequest { UnitCode = i.UnitCode, Title = i.Title }).ToList();
        if (unitItems.Count > 0) await _unitValidator.ValidateUnitCodesAsync(unitItems, ct);

        template.Update(req.Name, req.Description);

        // Remove old items
        _db.ProviderOfferTemplateItems.RemoveRange(template.Items);

        var newItems = req.Items.Select((i, idx) => ProviderOfferTemplateItemEntity.Create(
            i.ItemType, i.Title, i.Description, i.Quantity, i.UnitCode, i.UnitPrice, i.CurrencyCode,
            i.TaxRate, i.DiscountType, i.DiscountValue, i.SortOrder > 0 ? i.SortOrder : idx));
        template.ReplaceItems(newItems);

        await _db.SaveChangesAsync(ct);
        return new ProviderOfferTemplateDto
        {
            Id = template.Id, Name = template.Name, Description = template.Description,
            Items = template.Items.Select(i => new ProviderOfferTemplateItemDto
            {
                Id = i.Id, ItemType = i.ItemType, Title = i.Title, Description = i.Description,
                Quantity = i.Quantity, UnitCode = i.UnitCode, UnitPrice = i.UnitPrice, CurrencyCode = i.CurrencyCode,
                TaxRate = i.TaxRate, DiscountType = i.DiscountType, DiscountValue = i.DiscountValue, SortOrder = i.SortOrder
            }).ToList()
        };
    }
}

public sealed class DeleteTemplateCommandHandler : AizenCommandHandler<DeleteTemplateCommand, bool>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public DeleteTemplateCommandHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    { _db = db; _info = info; }

    public override async Task<bool> Handle(DeleteTemplateCommand command, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        var template = await _db.ProviderOfferTemplates
            .FirstOrDefaultAsync(t => t.Id == command.TemplateId && t.ProviderProfileId == profileId && !t.IsDeleted, ct)
            ?? throw new AizenBusinessException("Template not found.");

        template.Deactivate();
        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class ListTemplatesQueryHandler : AizenQueryHandler<ListTemplatesQuery, List<ProviderOfferTemplateDto>>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public ListTemplatesQueryHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    { _db = db; _info = info; }

    public override async Task<List<ProviderOfferTemplateDto>?> Handle(ListTemplatesQuery query, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        return await _db.ProviderOfferTemplates.AsNoTracking()
            .Include(t => t.Items.OrderBy(i => i.SortOrder))
            .Where(t => t.ProviderProfileId == profileId && t.IsActive && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .Select(t => new ProviderOfferTemplateDto
            {
                Id = t.Id, Name = t.Name, Description = t.Description,
                Items = t.Items.Select(i => new ProviderOfferTemplateItemDto
                {
                    Id = i.Id, ItemType = i.ItemType, Title = i.Title, Description = i.Description,
                    Quantity = i.Quantity, UnitCode = i.UnitCode, UnitPrice = i.UnitPrice, CurrencyCode = i.CurrencyCode,
                    TaxRate = i.TaxRate, DiscountType = i.DiscountType, DiscountValue = i.DiscountValue, SortOrder = i.SortOrder
                }).ToList()
            }).ToListAsync(ct);
    }
}

public sealed class GetTemplateQueryHandler : AizenQueryHandler<GetTemplateQuery, ProviderOfferTemplateDto>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public GetTemplateQueryHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    { _db = db; _info = info; }

    public override async Task<ProviderOfferTemplateDto?> Handle(GetTemplateQuery query, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        var t = await _db.ProviderOfferTemplates.AsNoTracking()
            .Include(t => t.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == query.TemplateId && t.ProviderProfileId == profileId && !t.IsDeleted, ct)
            ?? throw new AizenBusinessException("Template not found.");

        return new ProviderOfferTemplateDto
        {
            Id = t.Id, Name = t.Name, Description = t.Description,
            Items = t.Items.Select(i => new ProviderOfferTemplateItemDto
            {
                Id = i.Id, ItemType = i.ItemType, Title = i.Title, Description = i.Description,
                Quantity = i.Quantity, UnitCode = i.UnitCode, UnitPrice = i.UnitPrice, CurrencyCode = i.CurrencyCode,
                TaxRate = i.TaxRate, DiscountType = i.DiscountType, DiscountValue = i.DiscountValue, SortOrder = i.SortOrder
            }).ToList()
        };
    }
}
