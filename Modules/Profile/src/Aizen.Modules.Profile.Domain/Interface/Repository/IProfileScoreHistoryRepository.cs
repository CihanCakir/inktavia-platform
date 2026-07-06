using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;

namespace Aizen.Modules.Profile.Domain.Interface.Repository;

/// <summary>
/// Append-only repository. Only AddAsync is exposed — no Update or Delete.
/// </summary>
public interface IProfileScoreHistoryRepository
{
    Task<List<ProfileScoreHistoryEntity>> GetByProfileAsync(
        long profileId, ProfileType profileType,
        int skip, int take, CancellationToken ct);

    Task<int> CountByProfileAsync(long profileId, ProfileType profileType, CancellationToken ct);
    Task AddAsync(ProfileScoreHistoryEntity entity, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
