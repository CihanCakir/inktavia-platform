using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Catalog;

// ── Queries ──────────────────────────────────────────────────────────────────
public sealed class SearchVesselBrandsQuery : AizenQuery<IReadOnlyList<VesselBrandDto>>
{
    public SearchVesselBrandsQuery(string? search, bool onlyActive, int take) { Search = search; OnlyActive = onlyActive; Take = take; }
    public string? Search { get; } public bool OnlyActive { get; } public int Take { get; }
}
public sealed class SearchVesselBrandsQueryHandler : AizenQueryHandler<SearchVesselBrandsQuery, IReadOnlyList<VesselBrandDto>>
{
    private readonly IVesselCatalogReferenceService _svc;
    public SearchVesselBrandsQueryHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override Task<IReadOnlyList<VesselBrandDto>> Handle(SearchVesselBrandsQuery r, CancellationToken ct)
        => _svc.SearchBrandsAsync(r.Search, r.OnlyActive, r.Take, ct);
}

public sealed class GetVesselModelsQuery : AizenQuery<IReadOnlyList<VesselModelDto>>
{
    public GetVesselModelsQuery(long brandId, string? search, string? typeCode, bool onlyActive, int take)
    { BrandId = brandId; Search = search; TypeCode = typeCode; OnlyActive = onlyActive; Take = take; }
    public long BrandId { get; } public string? Search { get; } public string? TypeCode { get; } public bool OnlyActive { get; } public int Take { get; }
}
public sealed class GetVesselModelsQueryHandler : AizenQueryHandler<GetVesselModelsQuery, IReadOnlyList<VesselModelDto>>
{
    private readonly IVesselCatalogReferenceService _svc;
    public GetVesselModelsQueryHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override Task<IReadOnlyList<VesselModelDto>> Handle(GetVesselModelsQuery r, CancellationToken ct)
        => _svc.GetModelsAsync(r.BrandId, r.Search, r.TypeCode, r.OnlyActive, r.Take, ct);
}

public sealed class GetVesselModelByIdQuery : AizenQuery<VesselModelDto?>
{
    public GetVesselModelByIdQuery(long id) => Id = id; public long Id { get; }
}
public sealed class GetVesselModelByIdQueryHandler : AizenQueryHandler<GetVesselModelByIdQuery, VesselModelDto?>
{
    private readonly IVesselCatalogReferenceService _svc;
    public GetVesselModelByIdQueryHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override Task<VesselModelDto?> Handle(GetVesselModelByIdQuery r, CancellationToken ct) => _svc.GetModelByIdAsync(r.Id, ct);
}

public sealed class ListVesselBrandsForReviewQuery : AizenQuery<IReadOnlyList<VesselBrandDto>>
{
    public ListVesselBrandsForReviewQuery(int skip, int take) { Skip = skip; Take = take; } public int Skip { get; } public int Take { get; }
}
public sealed class ListVesselBrandsForReviewQueryHandler : AizenQueryHandler<ListVesselBrandsForReviewQuery, IReadOnlyList<VesselBrandDto>>
{
    private readonly IVesselCatalogReferenceService _svc;
    public ListVesselBrandsForReviewQueryHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override Task<IReadOnlyList<VesselBrandDto>> Handle(ListVesselBrandsForReviewQuery r, CancellationToken ct) => _svc.ListBrandsForReviewAsync(r.Skip, r.Take, ct);
}

public sealed class ListVesselModelsForReviewQuery : AizenQuery<IReadOnlyList<VesselModelDto>>
{
    public ListVesselModelsForReviewQuery(int skip, int take) { Skip = skip; Take = take; } public int Skip { get; } public int Take { get; }
}
public sealed class ListVesselModelsForReviewQueryHandler : AizenQueryHandler<ListVesselModelsForReviewQuery, IReadOnlyList<VesselModelDto>>
{
    private readonly IVesselCatalogReferenceService _svc;
    public ListVesselModelsForReviewQueryHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override Task<IReadOnlyList<VesselModelDto>> Handle(ListVesselModelsForReviewQuery r, CancellationToken ct) => _svc.ListModelsForReviewAsync(r.Skip, r.Take, ct);
}

// ── Commands ─────────────────────────────────────────────────────────────────
public sealed class SubmitVesselBrandCommand : AizenCommand<VesselBrandDto>
{ public SubmitVesselBrandCommand(SubmitVesselBrandRequest req) => Request = req; public SubmitVesselBrandRequest Request { get; } }
public sealed class SubmitVesselBrandCommandHandler : AizenCommandHandler<SubmitVesselBrandCommand, VesselBrandDto>
{
    private readonly IVesselCatalogReferenceService _svc; public SubmitVesselBrandCommandHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override async Task<VesselBrandDto?> Handle(SubmitVesselBrandCommand r, CancellationToken ct) => await _svc.SubmitBrandAsync(r.Request, ct);
}

public sealed class SubmitVesselModelCommand : AizenCommand<VesselModelDto>
{ public SubmitVesselModelCommand(SubmitVesselModelRequest req) => Request = req; public SubmitVesselModelRequest Request { get; } }
public sealed class SubmitVesselModelCommandHandler : AizenCommandHandler<SubmitVesselModelCommand, VesselModelDto>
{
    private readonly IVesselCatalogReferenceService _svc; public SubmitVesselModelCommandHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override async Task<VesselModelDto?> Handle(SubmitVesselModelCommand r, CancellationToken ct) => await _svc.SubmitModelAsync(r.Request, ct);
}

public sealed class CreateVesselBrandCommand : AizenCommand<VesselBrandDto>
{ public CreateVesselBrandCommand(CreateVesselBrandRequest req) => Request = req; public CreateVesselBrandRequest Request { get; } }
public sealed class CreateVesselBrandCommandHandler : AizenCommandHandler<CreateVesselBrandCommand, VesselBrandDto>
{
    private readonly IVesselCatalogReferenceService _svc; public CreateVesselBrandCommandHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override async Task<VesselBrandDto?> Handle(CreateVesselBrandCommand r, CancellationToken ct) => await _svc.CreateBrandAsync(r.Request, ct);
}

public sealed class UpdateVesselBrandCommand : AizenCommand<VesselBrandDto>
{ public UpdateVesselBrandCommand(long id, UpdateVesselBrandRequest req) { Id = id; Request = req; } public long Id { get; } public UpdateVesselBrandRequest Request { get; } }
public sealed class UpdateVesselBrandCommandHandler : AizenCommandHandler<UpdateVesselBrandCommand, VesselBrandDto>
{
    private readonly IVesselCatalogReferenceService _svc; public UpdateVesselBrandCommandHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override async Task<VesselBrandDto?> Handle(UpdateVesselBrandCommand r, CancellationToken ct) => await _svc.UpdateBrandAsync(r.Id, r.Request, ct);
}

public sealed class CreateVesselModelCommand : AizenCommand<VesselModelDto>
{ public CreateVesselModelCommand(CreateVesselModelRequest req) => Request = req; public CreateVesselModelRequest Request { get; } }
public sealed class CreateVesselModelCommandHandler : AizenCommandHandler<CreateVesselModelCommand, VesselModelDto>
{
    private readonly IVesselCatalogReferenceService _svc; public CreateVesselModelCommandHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override async Task<VesselModelDto?> Handle(CreateVesselModelCommand r, CancellationToken ct) => await _svc.CreateModelAsync(r.Request, ct);
}

public sealed class UpdateVesselModelCommand : AizenCommand<VesselModelDto>
{ public UpdateVesselModelCommand(long id, UpdateVesselModelRequest req) { Id = id; Request = req; } public long Id { get; } public UpdateVesselModelRequest Request { get; } }
public sealed class UpdateVesselModelCommandHandler : AizenCommandHandler<UpdateVesselModelCommand, VesselModelDto>
{
    private readonly IVesselCatalogReferenceService _svc; public UpdateVesselModelCommandHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override async Task<VesselModelDto?> Handle(UpdateVesselModelCommand r, CancellationToken ct) => await _svc.UpdateModelAsync(r.Id, r.Request, ct);
}

public sealed class ApproveVesselBrandCommand : AizenCommand<BoolResult>
{ public ApproveVesselBrandCommand(long id) => Id = id; public long Id { get; } }
public sealed class ApproveVesselBrandCommandHandler : AizenCommandHandler<ApproveVesselBrandCommand, BoolResult>
{
    private readonly IVesselCatalogReferenceService _svc; public ApproveVesselBrandCommandHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(ApproveVesselBrandCommand r, CancellationToken ct) { await _svc.ApproveBrandAsync(r.Id, ct); return new BoolResult(true); }
}

public sealed class ApproveVesselModelCommand : AizenCommand<BoolResult>
{ public ApproveVesselModelCommand(long id) => Id = id; public long Id { get; } }
public sealed class ApproveVesselModelCommandHandler : AizenCommandHandler<ApproveVesselModelCommand, BoolResult>
{
    private readonly IVesselCatalogReferenceService _svc; public ApproveVesselModelCommandHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(ApproveVesselModelCommand r, CancellationToken ct) { await _svc.ApproveModelAsync(r.Id, ct); return new BoolResult(true); }
}

public sealed class SetVesselBrandActiveCommand : AizenCommand<BoolResult>
{ public SetVesselBrandActiveCommand(long id, bool active) { Id = id; Active = active; } public long Id { get; } public bool Active { get; } }
public sealed class SetVesselBrandActiveCommandHandler : AizenCommandHandler<SetVesselBrandActiveCommand, BoolResult>
{
    private readonly IVesselCatalogReferenceService _svc; public SetVesselBrandActiveCommandHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(SetVesselBrandActiveCommand r, CancellationToken ct) { await _svc.SetBrandActiveAsync(r.Id, r.Active, ct); return new BoolResult(true); }
}

public sealed class SetVesselModelActiveCommand : AizenCommand<BoolResult>
{ public SetVesselModelActiveCommand(long id, bool active) { Id = id; Active = active; } public long Id { get; } public bool Active { get; } }
public sealed class SetVesselModelActiveCommandHandler : AizenCommandHandler<SetVesselModelActiveCommand, BoolResult>
{
    private readonly IVesselCatalogReferenceService _svc; public SetVesselModelActiveCommandHandler(IVesselCatalogReferenceService svc) => _svc = svc;
    public override async Task<BoolResult?> Handle(SetVesselModelActiveCommand r, CancellationToken ct) { await _svc.SetModelActiveAsync(r.Id, r.Active, ct); return new BoolResult(true); }
}
