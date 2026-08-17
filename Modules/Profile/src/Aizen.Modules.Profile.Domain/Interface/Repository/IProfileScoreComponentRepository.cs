using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;

namespace Aizen.Modules.Profile.Domain.Interface.Repository;

public interface IProfileScoreComponentRepository
{
    Task<List<ProfileScoreComponentEntity>> GetBySnapshotIdAsync(long snapshotId, CancellationToken ct);
    Task<List<ProfileScoreComponentEntity>> GetByProfileAsync(long profileId, ProfileType profileType, CancellationToken ct);
    /// <summary>Replaces all components for a snapshot in a single operation (delete-then-insert).</summary>
    Task ReplaceForSnapshotAsync(long snapshotId, IEnumerable<ProfileScoreComponentEntity> components, CancellationToken ct);
    Task AddAsync(ProfileScoreComponentEntity entity, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
