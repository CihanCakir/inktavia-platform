using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

/// <summary>
/// Batch-loads the requested profiles in ONE query (no N+1) and projects each to its display name:
/// <c>CompanyName</c> when set, otherwise <c>"{FirstName} {LastName}".Trim()</c>, else null. Ids are deduped and
/// defensively capped. The display-name computation runs in memory (string interpolation isn't SQL-translatable);
/// only the three name columns are read from the DB.
/// </summary>
public sealed class GetProfileDisplayNamesByProfileIdsQueryHandler
    : AizenQueryHandler<GetProfileDisplayNamesByProfileIdsQuery, IList<ProfileDisplayNameDto>>
{
    private const int MaxIds = 500;

    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetProfileDisplayNamesByProfileIdsQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<IList<ProfileDisplayNameDto>> Handle(
        GetProfileDisplayNamesByProfileIdsQuery request, CancellationToken cancellationToken)
    {
        var ids = request.ProfileIds.Where(id => id > 0).Distinct().Take(MaxIds).ToArray();
        if (ids.Length == 0)
            return new List<ProfileDisplayNameDto>();

        var repo = _uow.GetRepository<UserProfileEntity>();

        // Read only the name columns for the requested ids — one round trip.
        var rows = (await repo.GetAllAsync<NameRow>(
            selector: p => new NameRow
            {
                ProfileId = p.Id,
                CompanyName = p.CompanyName,
                FirstName = p.FirstName,
                LastName = p.LastName,
            },
            predicate: p => !p.IsDeleted && ids.Contains(p.Id))).ToList();

        return rows
            .Select(r => new ProfileDisplayNameDto { ProfileId = r.ProfileId, DisplayName = ToDisplayName(r) })
            .ToList();
    }

    private static string? ToDisplayName(NameRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.CompanyName))
            return r.CompanyName!.Trim();

        var person = $"{r.FirstName} {r.LastName}".Trim();
        return string.IsNullOrWhiteSpace(person) ? null : person;
    }

    // EF projection target — the minimal set of columns the display name is derived from.
    private sealed class NameRow
    {
        public long ProfileId { get; set; }
        public string? CompanyName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
