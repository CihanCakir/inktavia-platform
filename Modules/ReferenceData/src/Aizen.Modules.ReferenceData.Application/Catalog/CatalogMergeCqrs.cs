using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Catalog;

public sealed class MergeCatalogEntryCommand : AizenCommand<CatalogMergeResultDto>
{
    public MergeCatalogEntryCommand(CatalogMergeType type, long sourceId, long targetId)
    { Type = type; SourceId = sourceId; TargetId = targetId; }
    public CatalogMergeType Type { get; } public long SourceId { get; } public long TargetId { get; }
}

public sealed class MergeCatalogEntryCommandHandler : AizenCommandHandler<MergeCatalogEntryCommand, CatalogMergeResultDto>
{
    private readonly ICatalogMergeService _svc;
    public MergeCatalogEntryCommandHandler(ICatalogMergeService svc) => _svc = svc;
    public override async Task<CatalogMergeResultDto?> Handle(MergeCatalogEntryCommand r, CancellationToken ct)
        => await _svc.MergeAsync(r.Type, r.SourceId, r.TargetId, ct);
}
