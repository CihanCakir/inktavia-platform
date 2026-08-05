using Aizen.Modules.ReferenceData.Repository.Mappings;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Abstraction.Request.Measurement;
using Aizen.Modules.ReferenceData.Domain.Entities.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class MeasurementReferenceService : IMeasurementReferenceService
{
    private readonly IMeasurementUnitRepository _repo;
    private readonly ReferenceDataDbContext _dbContext;

    public MeasurementReferenceService(IMeasurementUnitRepository repo, ReferenceDataDbContext dbContext)
    {
        _repo = repo;
        _dbContext = dbContext;
    }

    public async Task<MeasurementUnitDto> CreateAsync(CreateMeasurementUnitRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _repo.ExistsByCodeAsync(request.Code, cancellationToken);
        if (exists) throw new AizenBusinessException($"Measurement unit with code '{request.Code}' already exists.");

        var entity = MeasurementUnitEntity.Create(request.Code, request.Name, request.Symbol, request.UnitType, request.ConversionFactorToBase, request.BaseUnitCode);
        await _repo.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<MeasurementUnitDto> UpdateAsync(long id, string name, string symbol, decimal? conversionFactorToBase, string? baseUnitCode, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Measurement unit with id '{id}' not found.");
        entity.Update(name, symbol, conversionFactorToBase, baseUnitCode, isActive);
        _repo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task ActivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Measurement unit with id '{id}' not found.");
        entity.Activate();
        _repo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Measurement unit with id '{id}' not found.");
        entity.Deactivate();
        _repo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MeasurementUnitDto>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetListAsync(onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<MeasurementUnitDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken);
        return entity?.ToDto();
    }

    public async Task<MeasurementUnitDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByCodeAsync(code, cancellationToken);
        return entity?.ToDto();
    }

    public async Task<IReadOnlyList<MeasurementUnitDto>> GetByTypeAsync(MeasurementUnitType unitType, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetByTypeAsync(unitType, onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }
}
