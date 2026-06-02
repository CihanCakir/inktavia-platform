using Aizen.Modules.ReferenceData.Repository.Mappings;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Domain.Entities.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class SystemParameterReferenceService : ISystemParameterReferenceService
{
    private readonly ISystemParameterRepository _repo;
    private readonly ReferenceDataDbContext _dbContext;

    public SystemParameterReferenceService(ISystemParameterRepository repo, ReferenceDataDbContext dbContext)
    {
        _repo = repo;
        _dbContext = dbContext;
    }

    public async Task<SystemParameterDto> CreateAsync(string key, string value, SystemParameterValueType valueType, string? description, bool isEncrypted, CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetByKeyAsync(key, cancellationToken);
        if (existing != null) throw new AizenBusinessException($"System parameter with key '{key}' already exists.");

        var entity = SystemParameterEntity.Create(key, value, valueType, description, isEncrypted);
        await _repo.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<SystemParameterDto> UpdateAsync(string key, string value, string? description, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByKeyAsync(key, cancellationToken)
            ?? throw new AizenBusinessException($"System parameter with key '{key}' not found.");
        entity.Update(value, description, isActive);
        _repo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task ActivateAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByKeyAsync(key, cancellationToken)
            ?? throw new AizenBusinessException($"System parameter with key '{key}' not found.");
        entity.Activate();
        _repo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByKeyAsync(key, cancellationToken)
            ?? throw new AizenBusinessException($"System parameter with key '{key}' not found.");
        entity.Deactivate();
        _repo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SystemParameterDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByKeyAsync(key, cancellationToken);
        return entity?.ToDto();
    }

    public async Task<IReadOnlyList<SystemParameterDto>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetListAsync(onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<SystemParameterDto>> GetByPrefixAsync(string prefix, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetByPrefixAsync(prefix, onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByKeyAsync(key, cancellationToken);
        return entity?.IsActive == true ? entity.Value : null;
    }

    public async Task<bool?> GetBooleanAsync(string key, CancellationToken cancellationToken = default)
    {
        var val = await GetStringAsync(key, cancellationToken);
        if (val == null) return null;
        return bool.TryParse(val, out var result) ? result : null;
    }

    public async Task<int?> GetIntAsync(string key, CancellationToken cancellationToken = default)
    {
        var val = await GetStringAsync(key, cancellationToken);
        if (val == null) return null;
        return int.TryParse(val, out var result) ? result : null;
    }

    public async Task<decimal?> GetDecimalAsync(string key, CancellationToken cancellationToken = default)
    {
        var val = await GetStringAsync(key, cancellationToken);
        if (val == null) return null;
        return decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
    }
}
