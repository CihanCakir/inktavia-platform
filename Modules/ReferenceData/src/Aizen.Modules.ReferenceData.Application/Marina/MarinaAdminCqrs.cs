using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;   // BoolResult
using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Marina;

// ── Query: admin curation list ───────────────────────────────────────────────
public sealed class ListMarinasForAdminQuery : AizenQuery<MarinaAdminListResult>
{
    public ListMarinasForAdminQuery(bool needsReviewOnly, string? search, int page, int pageSize)
    { NeedsReviewOnly = needsReviewOnly; Search = search; Page = page; PageSize = pageSize; }
    public bool NeedsReviewOnly { get; }
    public string? Search { get; }
    public int Page { get; }
    public int PageSize { get; }
}
public sealed class ListMarinasForAdminQueryHandler : AizenQueryHandler<ListMarinasForAdminQuery, MarinaAdminListResult>
{
    private readonly IMarinaReferenceService _svc;
    public ListMarinasForAdminQueryHandler(IMarinaReferenceService svc) => _svc = svc;
    public override Task<MarinaAdminListResult> Handle(ListMarinasForAdminQuery r, CancellationToken ct)
        => _svc.ListForAdminAsync(r.NeedsReviewOnly, r.Search, r.Page, r.PageSize, ct);
}

// ── Commands: update / mark-reviewed / deactivate ────────────────────────────
public sealed class UpdateMarinaCommand : AizenCommand<BoolResult>
{
    public UpdateMarinaCommand(long id, string name, string? cityCode) { Id = id; Name = name; CityCode = cityCode; }
    public long Id { get; } public string Name { get; } public string? CityCode { get; }
}
public sealed class UpdateMarinaCommandHandler : AizenCommandHandler<UpdateMarinaCommand, BoolResult>
{
    private readonly IMarinaReferenceService _svc;
    public UpdateMarinaCommandHandler(IMarinaReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(UpdateMarinaCommand r, CancellationToken ct)
        => new BoolResult(await _svc.UpdateAsync(r.Id, r.Name, r.CityCode, ct));
}

public sealed class MarkMarinaReviewedCommand : AizenCommand<BoolResult>
{
    public MarkMarinaReviewedCommand(long id) => Id = id; public long Id { get; }
}
public sealed class MarkMarinaReviewedCommandHandler : AizenCommandHandler<MarkMarinaReviewedCommand, BoolResult>
{
    private readonly IMarinaReferenceService _svc;
    public MarkMarinaReviewedCommandHandler(IMarinaReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(MarkMarinaReviewedCommand r, CancellationToken ct)
        => new BoolResult(await _svc.MarkReviewedAsync(r.Id, ct));
}

public sealed class DeactivateMarinaCommand : AizenCommand<BoolResult>
{
    public DeactivateMarinaCommand(long id) => Id = id; public long Id { get; }
}
public sealed class DeactivateMarinaCommandHandler : AizenCommandHandler<DeactivateMarinaCommand, BoolResult>
{
    private readonly IMarinaReferenceService _svc;
    public DeactivateMarinaCommandHandler(IMarinaReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(DeactivateMarinaCommand r, CancellationToken ct)
        => new BoolResult(await _svc.DeactivateAsync(r.Id, ct));
}
