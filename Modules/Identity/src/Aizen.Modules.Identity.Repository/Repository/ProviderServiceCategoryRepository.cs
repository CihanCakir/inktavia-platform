using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities.ProviderServiceCategory;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Identity.Repository.Identity.Repository;

public sealed class ProviderServiceCategoryRepository : IProviderServiceCategoryRepository
{
    private readonly IdentityDbContext _db;

    public ProviderServiceCategoryRepository(IdentityDbContext db) => _db = db;

    public async Task ReplaceForProfileAsync(
        long profileId, long userId, IEnumerable<string> serviceCategoryCodes, CancellationToken ct = default)
    {
        var existing = await _db.ProviderServiceCategories.Where(x => x.ProfileId == profileId).ToListAsync(ct);
        if (existing.Count > 0)
            _db.ProviderServiceCategories.RemoveRange(existing);

        var normalized = serviceCategoryCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        foreach (var code in normalized)
            await _db.ProviderServiceCategories.AddAsync(
                ProviderServiceCategoryEntity.Create(profileId, userId, code), ct);
        // Caller owns the unit-of-work commit (SaveChangesAsync).
    }

    public Task<bool> HasAnyForProfileAsync(long profileId, CancellationToken ct = default)
        => _db.ProviderServiceCategories.AnyAsync(x => x.ProfileId == profileId, ct);

    public async Task<List<ProviderAreaRow>> GetProvidersForAreaAsync(
        string cityCode, string? serviceCategoryCode, int take, CancellationToken ct = default)
    {
        var city = cityCode.Trim().ToUpperInvariant();

        // Approved + active organizer (provider) profiles operating in this city.
        var query = _db.UserProfiles.AsNoTracking()
            .Where(p => p.RoleContext == WorkshopRoleContext.Organizer
                     && p.ApprovalStatus == ApprovalStatus.Approved
                     && p.Status == ProfileStatus.Active
                     && !p.IsDeleted
                     && p.City == city);

        if (!string.IsNullOrWhiteSpace(serviceCategoryCode))
        {
            var cat = serviceCategoryCode.Trim().ToLowerInvariant();
            query = query.Where(p => _db.ProviderServiceCategories
                .Any(c => c.ProfileId == p.Id && c.ServiceCategoryCode == cat && !c.IsDeleted));
        }

        var rows = await query
            .OrderBy(p => p.Id)
            .Take(take)
            .Select(p => new { p.Id, p.UserId })
            .ToListAsync(ct);

        return rows.Select(r => new ProviderAreaRow(r.Id, r.UserId)).ToList();
    }
}
