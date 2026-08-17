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

public sealed class CreateCatalogItemCommandHandler : AizenCommandHandler<CreateCatalogItemCommand, ProviderCatalogItemDto>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;
    private readonly UnitCodeValidator _unitValidator;

    public CreateCatalogItemCommandHandler(ServiceRequestDbContext db, IAizenInfoAccessor info, UnitCodeValidator unitValidator)
    { _db = db; _info = info; _unitValidator = unitValidator; }

    public override async Task<ProviderCatalogItemDto?> Handle(CreateCatalogItemCommand command, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        var req = command.Request;
        ValidateInput(req);
        await ValidateUnitAsync(req, ct);

        var entity = ProviderCatalogItemEntity.Create(
            profileId, req.ItemType, req.Title, req.Description,
            req.DefaultQuantity, req.UnitCode, req.DefaultUnitPrice, req.CurrencyCode, req.DefaultTaxRate);

        _db.ProviderCatalogItems.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    private async Task ValidateUnitAsync(CatalogItemRequest req, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(req.UnitCode))
        {
            var items = new[] { new CreateServiceRequestOfferItemRequest { UnitCode = req.UnitCode, Title = req.Title } };
            await _unitValidator.ValidateUnitCodesAsync(items, ct);
        }
    }

    private static void ValidateInput(CatalogItemRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) throw new AizenBusinessException("Title is required.");
        if (req.DefaultUnitPrice < 0) throw new AizenBusinessException("Unit price must be >= 0.");
        if (req.DefaultTaxRate < 0 || req.DefaultTaxRate > 1) throw new AizenBusinessException("Tax rate must be between 0 and 1.");
    }

    private static ProviderCatalogItemDto ToDto(ProviderCatalogItemEntity e) => new()
    {
        Id = e.Id, ItemType = e.ItemType, Title = e.Title, Description = e.Description,
        DefaultQuantity = e.DefaultQuantity, UnitCode = e.UnitCode, DefaultUnitPrice = e.DefaultUnitPrice,
        CurrencyCode = e.CurrencyCode, DefaultTaxRate = e.DefaultTaxRate
    };
}

public sealed class UpdateCatalogItemCommandHandler : AizenCommandHandler<UpdateCatalogItemCommand, ProviderCatalogItemDto>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;
    private readonly UnitCodeValidator _unitValidator;

    public UpdateCatalogItemCommandHandler(ServiceRequestDbContext db, IAizenInfoAccessor info, UnitCodeValidator unitValidator)
    { _db = db; _info = info; _unitValidator = unitValidator; }

    public override async Task<ProviderCatalogItemDto?> Handle(UpdateCatalogItemCommand command, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        var entity = await _db.ProviderCatalogItems
            .FirstOrDefaultAsync(x => x.Id == command.ItemId && x.ProviderProfileId == profileId && !x.IsDeleted, ct)
            ?? throw new AizenBusinessException("Catalog item not found.");

        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Title)) throw new AizenBusinessException("Title is required.");
        if (req.DefaultUnitPrice < 0) throw new AizenBusinessException("Unit price must be >= 0.");
        if (req.DefaultTaxRate < 0 || req.DefaultTaxRate > 1) throw new AizenBusinessException("Tax rate must be between 0 and 1.");

        if (!string.IsNullOrWhiteSpace(req.UnitCode))
        {
            var items = new[] { new CreateServiceRequestOfferItemRequest { UnitCode = req.UnitCode, Title = req.Title } };
            await _unitValidator.ValidateUnitCodesAsync(items, ct);
        }

        entity.Update(req.ItemType, req.Title, req.Description, req.DefaultQuantity,
            req.UnitCode, req.DefaultUnitPrice, req.CurrencyCode, req.DefaultTaxRate);

        await _db.SaveChangesAsync(ct);
        return new ProviderCatalogItemDto
        {
            Id = entity.Id, ItemType = entity.ItemType, Title = entity.Title, Description = entity.Description,
            DefaultQuantity = entity.DefaultQuantity, UnitCode = entity.UnitCode, DefaultUnitPrice = entity.DefaultUnitPrice,
            CurrencyCode = entity.CurrencyCode, DefaultTaxRate = entity.DefaultTaxRate
        };
    }
}

public sealed class DeleteCatalogItemCommandHandler : AizenCommandHandler<DeleteCatalogItemCommand, bool>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public DeleteCatalogItemCommandHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    { _db = db; _info = info; }

    public override async Task<bool> Handle(DeleteCatalogItemCommand command, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        var entity = await _db.ProviderCatalogItems
            .FirstOrDefaultAsync(x => x.Id == command.ItemId && x.ProviderProfileId == profileId && !x.IsDeleted, ct)
            ?? throw new AizenBusinessException("Catalog item not found.");

        entity.Deactivate();
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class ListCatalogItemsQueryHandler : AizenQueryHandler<ListCatalogItemsQuery, List<ProviderCatalogItemDto>>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public ListCatalogItemsQueryHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    { _db = db; _info = info; }

    public override async Task<List<ProviderCatalogItemDto>?> Handle(ListCatalogItemsQuery query, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        return await _db.ProviderCatalogItems
            .AsNoTracking()
            .Where(x => x.ProviderProfileId == profileId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Title)
            .Select(x => new ProviderCatalogItemDto
            {
                Id = x.Id, ItemType = x.ItemType, Title = x.Title, Description = x.Description,
                DefaultQuantity = x.DefaultQuantity, UnitCode = x.UnitCode, DefaultUnitPrice = x.DefaultUnitPrice,
                CurrencyCode = x.CurrencyCode, DefaultTaxRate = x.DefaultTaxRate
            })
            .ToListAsync(ct);
    }
}
