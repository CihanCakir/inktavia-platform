using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;
using Aizen.Modules.Identity.Abstraction.Options;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Application.ProviderEligibility.GetProviderAreaAvailability;

public sealed class GetProviderAreaAvailabilityQueryHandler
    : AizenQueryHandler<GetProviderAreaAvailabilityQuery, ProviderAreaAvailabilityDto>
{
    // Coarse verdict values — the only thing that leaves the module.
    private const string None = "none";
    private const string Limited = "limited";
    private const string Available = "available";

    private readonly IProviderServiceCategoryRepository _repository;
    private readonly AvailabilityThresholdsOptions _thresholds;

    public GetProviderAreaAvailabilityQueryHandler(
        IProviderServiceCategoryRepository repository,
        IOptions<AvailabilityThresholdsOptions> thresholds)
    {
        _repository = repository;
        _thresholds = thresholds.Value;
    }

    public override async Task<ProviderAreaAvailabilityDto?> Handle(
        GetProviderAreaAvailabilityQuery request, CancellationToken ct)
    {
        var cityCode = request.CityCode?.Trim() ?? string.Empty;
        var categoryCode = string.IsNullOrWhiteSpace(request.CategoryCode) ? null : request.CategoryCode.Trim();

        if (string.IsNullOrWhiteSpace(cityCode))
            return Coarse(cityCode, categoryCode, None);

        // Config-driven bands (not compiled-in): fetch at most `cap` rows, then bucket. `cap` is the highest boundary
        // we need to distinguish, so a genuine count is never materialized — the repo returns an int in [0, cap].
        var availableMin = Math.Max(1, _thresholds.AvailableMin);
        var limitedMin = Math.Max(1, _thresholds.LimitedMin);
        var cap = Math.Max(availableMin, limitedMin);

        var count = await _repository.CountForAreaAsync(cityCode, categoryCode, cap, ct);

        var availability = count >= availableMin ? Available
            : count >= limitedMin ? Limited
            : None;

        return Coarse(cityCode, categoryCode, availability);
    }

    private static ProviderAreaAvailabilityDto Coarse(string cityCode, string? categoryCode, string availability)
        => new() { CityCode = cityCode, CategoryCode = categoryCode, Availability = availability };
}
