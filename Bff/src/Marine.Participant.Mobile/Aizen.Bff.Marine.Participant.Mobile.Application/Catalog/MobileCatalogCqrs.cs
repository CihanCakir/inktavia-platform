using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Catalog;

// The module catalog DTOs are already cost-free (no economics) — forwarded verbatim.

// ── Vessel reads ──
public sealed class GetMobileVesselBrandsQuery : AizenQuery<List<VesselBrandDto>>
{ public GetMobileVesselBrandsQuery(string? search, int take) { Search = search; Take = take; } public string? Search { get; } public int Take { get; } }
public sealed class GetMobileVesselBrandsQueryHandler : AizenQueryHandler<GetMobileVesselBrandsQuery, List<VesselBrandDto>>
{
    private readonly IReferenceDataRemoteCall _rd; public GetMobileVesselBrandsQueryHandler(IReferenceDataRemoteCall rd) => _rd = rd;
    public override async Task<List<VesselBrandDto>?> Handle(GetMobileVesselBrandsQuery r, CancellationToken ct)
        => (await _rd.GetVesselBrands(r.Search, true, r.Take))?.Body ?? new();
}

public sealed class GetMobileVesselModelsQuery : AizenQuery<List<VesselModelDto>>
{ public GetMobileVesselModelsQuery(long brandId, string? search, string? typeCode, int take) { BrandId = brandId; Search = search; TypeCode = typeCode; Take = take; } public long BrandId { get; } public string? Search { get; } public string? TypeCode { get; } public int Take { get; } }
public sealed class GetMobileVesselModelsQueryHandler : AizenQueryHandler<GetMobileVesselModelsQuery, List<VesselModelDto>>
{
    private readonly IReferenceDataRemoteCall _rd; public GetMobileVesselModelsQueryHandler(IReferenceDataRemoteCall rd) => _rd = rd;
    public override async Task<List<VesselModelDto>?> Handle(GetMobileVesselModelsQuery r, CancellationToken ct)
        => (await _rd.GetVesselModels(r.BrandId, r.Search, r.TypeCode, true, r.Take))?.Body ?? new();
}

// ── Engine reads ──
public sealed class GetMobileEngineBrandsQuery : AizenQuery<List<EngineBrandDto>>
{ public GetMobileEngineBrandsQuery(string? search, int take) { Search = search; Take = take; } public string? Search { get; } public int Take { get; } }
public sealed class GetMobileEngineBrandsQueryHandler : AizenQueryHandler<GetMobileEngineBrandsQuery, List<EngineBrandDto>>
{
    private readonly IReferenceDataRemoteCall _rd; public GetMobileEngineBrandsQueryHandler(IReferenceDataRemoteCall rd) => _rd = rd;
    public override async Task<List<EngineBrandDto>?> Handle(GetMobileEngineBrandsQuery r, CancellationToken ct)
        => (await _rd.GetEngineBrands(r.Search, true, r.Take))?.Body ?? new();
}

public sealed class GetMobileEngineModelsQuery : AizenQuery<List<EngineModelDto>>
{ public GetMobileEngineModelsQuery(long brandId, string? search, string? typeCode, int take) { BrandId = brandId; Search = search; TypeCode = typeCode; Take = take; } public long BrandId { get; } public string? Search { get; } public string? TypeCode { get; } public int Take { get; } }
public sealed class GetMobileEngineModelsQueryHandler : AizenQueryHandler<GetMobileEngineModelsQuery, List<EngineModelDto>>
{
    private readonly IReferenceDataRemoteCall _rd; public GetMobileEngineModelsQueryHandler(IReferenceDataRemoteCall rd) => _rd = rd;
    public override async Task<List<EngineModelDto>?> Handle(GetMobileEngineModelsQuery r, CancellationToken ct)
        => (await _rd.GetEngineModels(r.BrandId, r.Search, r.TypeCode, true, r.Take))?.Body ?? new();
}

// ── "Not in list" submissions ──
public sealed class SubmitMobileVesselBrandCommand : AizenCommand<VesselBrandDto>
{ public SubmitMobileVesselBrandCommand(SubmitVesselBrandRequest req) => Request = req; public SubmitVesselBrandRequest Request { get; } }
public sealed class SubmitMobileVesselBrandCommandHandler : AizenCommandHandler<SubmitMobileVesselBrandCommand, VesselBrandDto>
{
    private readonly IReferenceDataRemoteCall _rd; public SubmitMobileVesselBrandCommandHandler(IReferenceDataRemoteCall rd) => _rd = rd;
    public override async Task<VesselBrandDto?> Handle(SubmitMobileVesselBrandCommand r, CancellationToken ct) => (await _rd.SubmitVesselBrand(r.Request))?.Body;
}

public sealed class SubmitMobileVesselModelCommand : AizenCommand<VesselModelDto>
{ public SubmitMobileVesselModelCommand(long brandId, SubmitVesselModelRequest req) { BrandId = brandId; Request = req; } public long BrandId { get; } public SubmitVesselModelRequest Request { get; } }
public sealed class SubmitMobileVesselModelCommandHandler : AizenCommandHandler<SubmitMobileVesselModelCommand, VesselModelDto>
{
    private readonly IReferenceDataRemoteCall _rd; public SubmitMobileVesselModelCommandHandler(IReferenceDataRemoteCall rd) => _rd = rd;
    public override async Task<VesselModelDto?> Handle(SubmitMobileVesselModelCommand r, CancellationToken ct) => (await _rd.SubmitVesselModel(r.BrandId, r.Request))?.Body;
}

public sealed class SubmitMobileEngineBrandCommand : AizenCommand<EngineBrandDto>
{ public SubmitMobileEngineBrandCommand(SubmitEngineBrandRequest req) => Request = req; public SubmitEngineBrandRequest Request { get; } }
public sealed class SubmitMobileEngineBrandCommandHandler : AizenCommandHandler<SubmitMobileEngineBrandCommand, EngineBrandDto>
{
    private readonly IReferenceDataRemoteCall _rd; public SubmitMobileEngineBrandCommandHandler(IReferenceDataRemoteCall rd) => _rd = rd;
    public override async Task<EngineBrandDto?> Handle(SubmitMobileEngineBrandCommand r, CancellationToken ct) => (await _rd.SubmitEngineBrand(r.Request))?.Body;
}

public sealed class SubmitMobileEngineModelCommand : AizenCommand<EngineModelDto>
{ public SubmitMobileEngineModelCommand(long brandId, SubmitEngineModelRequest req) { BrandId = brandId; Request = req; } public long BrandId { get; } public SubmitEngineModelRequest Request { get; } }
public sealed class SubmitMobileEngineModelCommandHandler : AizenCommandHandler<SubmitMobileEngineModelCommand, EngineModelDto>
{
    private readonly IReferenceDataRemoteCall _rd; public SubmitMobileEngineModelCommandHandler(IReferenceDataRemoteCall rd) => _rd = rd;
    public override async Task<EngineModelDto?> Handle(SubmitMobileEngineModelCommand r, CancellationToken ct) => (await _rd.SubmitEngineModel(r.BrandId, r.Request))?.Body;
}
