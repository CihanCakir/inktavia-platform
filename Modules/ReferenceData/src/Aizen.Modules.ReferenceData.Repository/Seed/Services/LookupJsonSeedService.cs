using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Seed.Models.Lookup;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Services;

/// <summary>Seeds LookupGroup and LookupItem entities from JSON files.</summary>
[DocumentationInfo(
    "Seeds the full Inktavia Marine OS lookup group tree and all lookup items from JSON files.",
    "Groups are inserted in topological order (parents before children). Idempotency key: Code for groups, GroupCode+Code for items.")]
public sealed class LookupJsonSeedService
{
    private readonly ILookupGroupRepository _groupRepository;
    private readonly ILookupItemRepository _itemRepository;
    private readonly ReferenceDataDbContext _dbContext;
    private readonly IReferenceDataJsonSeedReader _reader;

    public LookupJsonSeedService(
        ILookupGroupRepository groupRepository,
        ILookupItemRepository itemRepository,
        ReferenceDataDbContext dbContext,
        IReferenceDataJsonSeedReader reader)
    {
        _groupRepository = groupRepository;
        _itemRepository = itemRepository;
        _dbContext = dbContext;
        _reader = reader;
    }

    public async Task SeedGroupsAsync(CancellationToken cancellationToken = default)
    {
        var models = await _reader.ReadListAsync<LookupGroupSeedModel>("Lookup/lookup-groups.json", cancellationToken: cancellationToken);

        // Build a map for quick code→entity lookup after insertion
        var codeToEntity = new Dictionary<string, LookupGroupEntity>(StringComparer.OrdinalIgnoreCase);

        // Load existing groups first
        var existingGroups = await _groupRepository.GetGroupsAsync(onlyActive: false, cancellationToken);
        foreach (var g in existingGroups)
            codeToEntity[g.Code] = g;

        // Process in topological order: roots first, then children
        var ordered = TopologicalSort(models);

        foreach (var model in ordered)
        {
            var normalizedCode = model.Code.Trim().ToUpperInvariant();
            var groupType = (LookupGroupType)model.GroupType;

            if (codeToEntity.TryGetValue(normalizedCode, out var existing))
            {
                existing.Update(model.Name, model.Description, groupType, model.SortOrder, model.IsActive);
                _groupRepository.Update(existing);
            }
            else
            {
                LookupGroupEntity entity;

                if (string.IsNullOrWhiteSpace(model.ParentCode))
                {
                    entity = LookupGroupEntity.CreateRoot(
                        normalizedCode, model.Name, model.Description, groupType, model.IsSystemGroup, model.SortOrder);
                }
                else
                {
                    var parentCode = model.ParentCode.Trim().ToUpperInvariant();
                    if (!codeToEntity.TryGetValue(parentCode, out var parent))
                        throw new InvalidOperationException($"Parent LookupGroup '{parentCode}' not found when seeding '{normalizedCode}'. Ensure parent is seeded first.");

                    entity = LookupGroupEntity.CreateChild(
                        parent.Id,
                        parent.HierarchyPath,
                        parent.Level,
                        normalizedCode,
                        model.Name,
                        model.Description,
                        groupType,
                        model.IsSystemGroup,
                        model.SortOrder);
                }

                await _groupRepository.AddAsync(entity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                codeToEntity[normalizedCode] = entity;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SeedItemsAsync(CancellationToken cancellationToken = default)
    {
        var models = await _reader.ReadListAsync<LookupItemSeedModel>("Lookup/lookup-items.json", cancellationToken: cancellationToken);

        foreach (var model in models)
        {
            var groupCode = model.GroupCode.Trim().ToUpperInvariant();
            var group = await _groupRepository.GetGroupByCodeAsync(groupCode, cancellationToken)
                        ?? throw new InvalidOperationException($"LookupGroup '{groupCode}' not found when seeding item '{model.Code}'.");

            var itemCode = model.Code.Trim().ToUpperInvariant();
            var exists = await _itemRepository.ExistsByCodeInGroupAsync(group.Id, itemCode, cancellationToken);

            if (!exists)
            {
                var entity = LookupItemEntity.Create(
                    group.Id,
                    itemCode,
                    model.Name,
                    model.Description,
                    model.IconKey,
                    model.ColorCode,
                    model.SortOrder,
                    model.IsDefault);

                if (!model.IsActive) entity.Deactivate();
                await _itemRepository.AddAsync(entity, cancellationToken);
            }
            else
            {
                var existing = await _itemRepository.GetItemByCodeAsync(itemCode, cancellationToken);
                if (existing is not null)
                {
                    existing.Update(model.Name, model.Description, model.IconKey, model.ColorCode, model.SortOrder, model.IsDefault, model.IsActive);
                    _itemRepository.Update(existing);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<LookupGroupSeedModel> TopologicalSort(IReadOnlyList<LookupGroupSeedModel> models)
    {
        var result = new List<LookupGroupSeedModel>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var map = models.ToDictionary(m => m.Code.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

        void Visit(LookupGroupSeedModel model)
        {
            var code = model.Code.Trim().ToUpperInvariant();
            if (visited.Contains(code)) return;
            visited.Add(code);

            if (!string.IsNullOrWhiteSpace(model.ParentCode))
            {
                var parentCode = model.ParentCode.Trim().ToUpperInvariant();
                if (map.TryGetValue(parentCode, out var parent))
                    Visit(parent);
            }

            result.Add(model);
        }

        foreach (var model in models)
            Visit(model);

        return result;
    }
}
