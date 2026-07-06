using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;

namespace Aizen.Modules.Profile.Domain.Interface.Repository;

public interface IProfileRiskSignalRepository
{
    Task<ProfileRiskSignalEntity?> GetByIdAsync(long id, CancellationToken ct);
    Task<List<ProfileRiskSignalEntity>> GetActiveByProfileAsync(long profileId, ProfileType profileType, CancellationToken ct);
    Task<List<ProfileRiskSignalEntity>> GetByProfileAsync(long profileId, ProfileType profileType, int skip, int take, CancellationToken ct);
    Task<bool> HasActiveSignalAboveAsync(long profileId, ProfileType profileType, RiskSignalSeverity minSeverity, CancellationToken ct);
    Task<RiskSignalSeverity?> GetMaxActiveSeverityAsync(long profileId, ProfileType profileType, CancellationToken ct);
    Task AddAsync(ProfileRiskSignalEntity entity, CancellationToken ct);
    void Update(ProfileRiskSignalEntity entity);
    Task SaveChangesAsync(CancellationToken ct);
}
