using Aizen.Modules.ReferenceData.Repository.Mappings;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class LookupTreeService : ILookupTreeService
{
    private readonly ILookupGroupRepository _groupRepo;
    private readonly ReferenceDataDbContext _dbContext;

    public LookupTreeService(ILookupGroupRepository groupRepo, ReferenceDataDbContext dbContext)
    {
        _groupRepo = groupRepo;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LookupGroupTreeDto>> GetTreeAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var all = await _groupRepo.GetGroupTreeAsync(onlyActive, cancellationToken);
        return BuildTree(null, all);
    }

    public async Task<LookupGroupTreeDto> MoveGroupAsync(MoveLookupGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepo.GetGroupByIdAsync(request.LookupGroupId, cancellationToken)
            ?? throw new AizenBusinessException($"Lookup group '{request.LookupGroupId}' not found.");

        if (request.NewParentLookupGroupId.HasValue)
        {
            var newParent = await _groupRepo.GetGroupByIdAsync(request.NewParentLookupGroupId.Value, cancellationToken)
                ?? throw new AizenBusinessException($"Target parent group '{request.NewParentLookupGroupId}' not found.");

            if (newParent.HierarchyPath.StartsWith(group.HierarchyPath + "/") || newParent.Id == group.Id)
                throw new AizenBusinessException("Cannot move a group under itself or its own descendant.");

            group.MoveToParent(newParent.Id, newParent.HierarchyPath, newParent.Level);
        }
        else
        {
            group.MoveToRoot();
        }

        _groupRepo.Update(group);

        var descendants = await _groupRepo.GetDescendantsAsync(group.HierarchyPath, cancellationToken);
        foreach (var desc in descendants)
        {
            var parts = desc.HierarchyPath.Split('/');
            var parentPath = string.Join("/", parts[..^1]);
            var parentLevel = parts.Length - 2;
            desc.RefreshHierarchy(parentPath, parentLevel);
            _groupRepo.Update(desc);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return group.ToTreeDto();
    }

    private static List<LookupGroupTreeDto> BuildTree(long? parentId, IReadOnlyList<Domain.Entities.LookupGroup.LookupGroupEntity> all)
    {
        return all
            .Where(x => x.ParentLookupGroupId == parentId)
            .OrderBy(x => x.SortOrder)
            .Select(x =>
            {
                var dto = x.ToTreeDto();
                var children = BuildTree(x.Id, all);
                return new LookupGroupTreeDto
                {
                    Id = dto.Id,
                    ParentLookupGroupId = dto.ParentLookupGroupId,
                    Code = dto.Code,
                    Name = dto.Name,
                    Description = dto.Description,
                    GroupType = dto.GroupType,
                    Level = dto.Level,
                    HierarchyPath = dto.HierarchyPath,
                    SortOrder = dto.SortOrder,
                    IsSystemGroup = dto.IsSystemGroup,
                    IsActive = dto.IsActive,
                    Children = children,
                    Items = dto.Items
                };
            })
            .ToList();
    }
}
