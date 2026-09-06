using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Catalog;

// ── Queries ──
public sealed class SearchEngineBrandsQuery : AizenQuery<IReadOnlyList<EngineBrandDto>>
{
    public SearchEngineBrandsQuery(string? search, bool onlyActive, int take) { Search = search; OnlyActive = onlyActive; Take = take; }
    public string? Search { get; } public bool OnlyActive { get; } public int Take { get; }
}
public sealed class SearchEngineBrandsQueryHandler : AizenQueryHandler<SearchEngineBrandsQuery, IReadOnlyList<EngineBrandDto>>
{
    private readonly IEngineCatalogReferenceService _svc; public SearchEngineBrandsQueryHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override Task<IReadOnlyList<EngineBrandDto>> Handle(SearchEngineBrandsQuery r, CancellationToken ct) => _svc.SearchBrandsAsync(r.Search, r.OnlyActive, r.Take, ct);
}

public sealed class GetEngineModelsQuery : AizenQuery<IReadOnlyList<EngineModelDto>>
{
    public GetEngineModelsQuery(long brandId, string? search, string? typeCode, bool onlyActive, int take)
    { BrandId = brandId; Search = search; TypeCode = typeCode; OnlyActive = onlyActive; Take = take; }
    public long BrandId { get; } public string? Search { get; } public string? TypeCode { get; } public bool OnlyActive { get; } public int Take { get; }
}
public sealed class GetEngineModelsQueryHandler : AizenQueryHandler<GetEngineModelsQuery, IReadOnlyList<EngineModelDto>>
{
    private readonly IEngineCatalogReferenceService _svc; public GetEngineModelsQueryHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override Task<IReadOnlyList<EngineModelDto>> Handle(GetEngineModelsQuery r, CancellationToken ct) => _svc.GetModelsAsync(r.BrandId, r.Search, r.TypeCode, r.OnlyActive, r.Take, ct);
}

public sealed class GetEngineModelByIdQuery : AizenQuery<EngineModelDto?>
{ public GetEngineModelByIdQuery(long id) => Id = id; public long Id { get; } }
public sealed class GetEngineModelByIdQueryHandler : AizenQueryHandler<GetEngineModelByIdQuery, EngineModelDto?>
{
    private readonly IEngineCatalogReferenceService _svc; public GetEngineModelByIdQueryHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override Task<EngineModelDto?> Handle(GetEngineModelByIdQuery r, CancellationToken ct) => _svc.GetModelByIdAsync(r.Id, ct);
}

public sealed class ListEngineBrandsForReviewQuery : AizenQuery<IReadOnlyList<EngineBrandDto>>
{ public ListEngineBrandsForReviewQuery(int skip, int take) { Skip = skip; Take = take; } public int Skip { get; } public int Take { get; } }
public sealed class ListEngineBrandsForReviewQueryHandler : AizenQueryHandler<ListEngineBrandsForReviewQuery, IReadOnlyList<EngineBrandDto>>
{
    private readonly IEngineCatalogReferenceService _svc; public ListEngineBrandsForReviewQueryHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override Task<IReadOnlyList<EngineBrandDto>> Handle(ListEngineBrandsForReviewQuery r, CancellationToken ct) => _svc.ListBrandsForReviewAsync(r.Skip, r.Take, ct);
}

public sealed class ListEngineModelsForReviewQuery : AizenQuery<IReadOnlyList<EngineModelDto>>
{ public ListEngineModelsForReviewQuery(int skip, int take) { Skip = skip; Take = take; } public int Skip { get; } public int Take { get; } }
public sealed class ListEngineModelsForReviewQueryHandler : AizenQueryHandler<ListEngineModelsForReviewQuery, IReadOnlyList<EngineModelDto>>
{
    private readonly IEngineCatalogReferenceService _svc; public ListEngineModelsForReviewQueryHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override Task<IReadOnlyList<EngineModelDto>> Handle(ListEngineModelsForReviewQuery r, CancellationToken ct) => _svc.ListModelsForReviewAsync(r.Skip, r.Take, ct);
}

// ── Commands ──
public sealed class SubmitEngineBrandCommand : AizenCommand<EngineBrandDto>
{ public SubmitEngineBrandCommand(SubmitEngineBrandRequest req) => Request = req; public SubmitEngineBrandRequest Request { get; } }
public sealed class SubmitEngineBrandCommandHandler : AizenCommandHandler<SubmitEngineBrandCommand, EngineBrandDto>
{
    private readonly IEngineCatalogReferenceService _svc; public SubmitEngineBrandCommandHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override async Task<EngineBrandDto?> Handle(SubmitEngineBrandCommand r, CancellationToken ct) => await _svc.SubmitBrandAsync(r.Request, ct);
}

public sealed class SubmitEngineModelCommand : AizenCommand<EngineModelDto>
{ public SubmitEngineModelCommand(SubmitEngineModelRequest req) => Request = req; public SubmitEngineModelRequest Request { get; } }
public sealed class SubmitEngineModelCommandHandler : AizenCommandHandler<SubmitEngineModelCommand, EngineModelDto>
{
    private readonly IEngineCatalogReferenceService _svc; public SubmitEngineModelCommandHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override async Task<EngineModelDto?> Handle(SubmitEngineModelCommand r, CancellationToken ct) => await _svc.SubmitModelAsync(r.Request, ct);
}

public sealed class CreateEngineBrandCommand : AizenCommand<EngineBrandDto>
{ public CreateEngineBrandCommand(CreateEngineBrandRequest req) => Request = req; public CreateEngineBrandRequest Request { get; } }
public sealed class CreateEngineBrandCommandHandler : AizenCommandHandler<CreateEngineBrandCommand, EngineBrandDto>
{
    private readonly IEngineCatalogReferenceService _svc; public CreateEngineBrandCommandHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override async Task<EngineBrandDto?> Handle(CreateEngineBrandCommand r, CancellationToken ct) => await _svc.CreateBrandAsync(r.Request, ct);
}

public sealed class UpdateEngineBrandCommand : AizenCommand<EngineBrandDto>
{ public UpdateEngineBrandCommand(long id, UpdateEngineBrandRequest req) { Id = id; Request = req; } public long Id { get; } public UpdateEngineBrandRequest Request { get; } }
public sealed class UpdateEngineBrandCommandHandler : AizenCommandHandler<UpdateEngineBrandCommand, EngineBrandDto>
{
    private readonly IEngineCatalogReferenceService _svc; public UpdateEngineBrandCommandHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override async Task<EngineBrandDto?> Handle(UpdateEngineBrandCommand r, CancellationToken ct) => await _svc.UpdateBrandAsync(r.Id, r.Request, ct);
}

public sealed class CreateEngineModelCommand : AizenCommand<EngineModelDto>
{ public CreateEngineModelCommand(CreateEngineModelRequest req) => Request = req; public CreateEngineModelRequest Request { get; } }
public sealed class CreateEngineModelCommandHandler : AizenCommandHandler<CreateEngineModelCommand, EngineModelDto>
{
    private readonly IEngineCatalogReferenceService _svc; public CreateEngineModelCommandHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override async Task<EngineModelDto?> Handle(CreateEngineModelCommand r, CancellationToken ct) => await _svc.CreateModelAsync(r.Request, ct);
}

public sealed class UpdateEngineModelCommand : AizenCommand<EngineModelDto>
{ public UpdateEngineModelCommand(long id, UpdateEngineModelRequest req) { Id = id; Request = req; } public long Id { get; } public UpdateEngineModelRequest Request { get; } }
public sealed class UpdateEngineModelCommandHandler : AizenCommandHandler<UpdateEngineModelCommand, EngineModelDto>
{
    private readonly IEngineCatalogReferenceService _svc; public UpdateEngineModelCommandHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override async Task<EngineModelDto?> Handle(UpdateEngineModelCommand r, CancellationToken ct) => await _svc.UpdateModelAsync(r.Id, r.Request, ct);
}

public sealed class ApproveEngineBrandCommand : AizenCommand<BoolResult>
{ public ApproveEngineBrandCommand(long id) => Id = id; public long Id { get; } }
public sealed class ApproveEngineBrandCommandHandler : AizenCommandHandler<ApproveEngineBrandCommand, BoolResult>
{
    private readonly IEngineCatalogReferenceService _svc; public ApproveEngineBrandCommandHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(ApproveEngineBrandCommand r, CancellationToken ct) { await _svc.ApproveBrandAsync(r.Id, ct); return new BoolResult(true); }
}

public sealed class ApproveEngineModelCommand : AizenCommand<BoolResult>
{ public ApproveEngineModelCommand(long id) => Id = id; public long Id { get; } }
public sealed class ApproveEngineModelCommandHandler : AizenCommandHandler<ApproveEngineModelCommand, BoolResult>
{
    private readonly IEngineCatalogReferenceService _svc; public ApproveEngineModelCommandHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(ApproveEngineModelCommand r, CancellationToken ct) { await _svc.ApproveModelAsync(r.Id, ct); return new BoolResult(true); }
}

public sealed class SetEngineBrandActiveCommand : AizenCommand<BoolResult>
{ public SetEngineBrandActiveCommand(long id, bool active) { Id = id; Active = active; } public long Id { get; } public bool Active { get; } }
public sealed class SetEngineBrandActiveCommandHandler : AizenCommandHandler<SetEngineBrandActiveCommand, BoolResult>
{
    private readonly IEngineCatalogReferenceService _svc; public SetEngineBrandActiveCommandHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(SetEngineBrandActiveCommand r, CancellationToken ct) { await _svc.SetBrandActiveAsync(r.Id, r.Active, ct); return new BoolResult(true); }
}

public sealed class SetEngineModelActiveCommand : AizenCommand<BoolResult>
{ public SetEngineModelActiveCommand(long id, bool active) { Id = id; Active = active; } public long Id { get; } public bool Active { get; } }
public sealed class SetEngineModelActiveCommandHandler : AizenCommandHandler<SetEngineModelActiveCommand, BoolResult>
{
    private readonly IEngineCatalogReferenceService _svc; public SetEngineModelActiveCommandHandler(IEngineCatalogReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(SetEngineModelActiveCommand r, CancellationToken ct) { await _svc.SetModelActiveAsync(r.Id, r.Active, ct); return new BoolResult(true); }
}
