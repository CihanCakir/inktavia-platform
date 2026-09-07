using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.CatalogReference;
using Aizen.Modules.Vessel.Domain.Interface.Repository;

namespace Aizen.Modules.Vessel.Application.CatalogReference;

// ── Query: grouped reference counts (admin catalog review enrichment) ──
public sealed class GetVesselCatalogReferenceCountsQuery : AizenQuery<CatalogReferenceCountsDto> { }
public sealed class GetVesselCatalogReferenceCountsQueryHandler
    : AizenQueryHandler<GetVesselCatalogReferenceCountsQuery, CatalogReferenceCountsDto>
{
    private readonly IVesselCatalogReferenceRepository _repo;
    public GetVesselCatalogReferenceCountsQueryHandler(IVesselCatalogReferenceRepository repo) => _repo = repo;
    public override Task<CatalogReferenceCountsDto> Handle(GetVesselCatalogReferenceCountsQuery r, CancellationToken ct)
        => _repo.GetReferenceCountsAsync(ct);
}

// ── Command: repoint references (catalog merge) ──
public sealed class RepointVesselCatalogReferenceCommand : AizenCommand<CatalogRepointResultDto>
{
    public RepointVesselCatalogReferenceCommand(CatalogRefKind kind, long sourceId, long targetId)
    { Kind = kind; SourceId = sourceId; TargetId = targetId; }
    public CatalogRefKind Kind { get; } public long SourceId { get; } public long TargetId { get; }
}
public sealed class RepointVesselCatalogReferenceCommandHandler
    : AizenCommandHandler<RepointVesselCatalogReferenceCommand, CatalogRepointResultDto>
{
    private readonly IVesselCatalogReferenceRepository _repo;
    public RepointVesselCatalogReferenceCommandHandler(IVesselCatalogReferenceRepository repo) => _repo = repo;
    public override async Task<CatalogRepointResultDto?> Handle(RepointVesselCatalogReferenceCommand r, CancellationToken ct)
        => new CatalogRepointResultDto { RepointedCount = await _repo.RepointAsync(r.Kind, r.SourceId, r.TargetId, ct) };
}
