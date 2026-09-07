using Aizen.Modules.ReferenceData.Domain.Entities.Marina;

namespace Aizen.Modules.ReferenceData.Domain.Interface;

public interface IMarinaRepository
{
    Task<MarinaEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<MarinaEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(MarinaEntity entity, CancellationToken cancellationToken = default);
    void Update(MarinaEntity entity);

    /// <summary>Admin curation list: optional needsReview filter + name/city/province search, paged. Returns (rows, total).</summary>
    Task<(IReadOnlyList<MarinaEntity> Items, int Total)> ListForAdminAsync(
        bool needsReviewOnly, string? search, int skip, int take, CancellationToken cancellationToken = default);
}
