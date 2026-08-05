using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;
using Aizen.Modules.Identity.Domain.Interface.Repository;

namespace Aizen.Modules.Identity.Application.ProviderEligibility.GetProvidersForArea;

public sealed class GetProvidersForAreaQueryHandler
    : AizenQueryHandler<GetProvidersForAreaQuery, IList<ProviderForAreaDto>>
{
    // Hard ceiling so a huge city can't return an unbounded fan-out list.
    private const int MaxTake = 1000;

    private readonly IProviderServiceCategoryRepository _repository;

    public GetProvidersForAreaQueryHandler(IProviderServiceCategoryRepository repository)
        => _repository = repository;

    public override async Task<IList<ProviderForAreaDto>> Handle(
        GetProvidersForAreaQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CityCode))
            return new List<ProviderForAreaDto>();

        var take = Math.Clamp(request.Take, 1, MaxTake);
        var rows = await _repository.GetProvidersForAreaAsync(request.CityCode, request.CategoryCode, take, ct);

        return rows
            .Select(r => new ProviderForAreaDto { ProfileId = r.ProfileId, UserId = r.UserId })
            .ToList();
    }
}
