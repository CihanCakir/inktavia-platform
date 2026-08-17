using Aizen.Modules.Identity.Abstraction.Options;
using Aizen.Modules.Identity.Application.ProviderEligibility.GetProviderAreaAvailability;
using Aizen.Modules.Identity.Domain.Entities.ProviderServiceCategory;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Application.UnitTests;

/// <summary>
/// M2 — the coarse availability bucketing. Config thresholds drive the verdict (not compiled ifs), the query fetches
/// at most AvailableMin rows (cap) so no real count is materialized, and only the enum is returned.
/// </summary>
public sealed class ProviderAreaAvailabilityHandlerTests
{
    private sealed class FakeRepo : IProviderServiceCategoryRepository
    {
        public int CountToReturn { get; set; }
        public int LastCap { get; private set; } = -1;
        public string? LastCity { get; private set; }
        public string? LastCategory { get; private set; }

        public Task<int> CountForAreaAsync(string cityCode, string? serviceCategoryCode, int cap, CancellationToken ct = default)
        {
            LastCity = cityCode;
            LastCategory = serviceCategoryCode;
            LastCap = cap;
            // Mirror the real repo's cap semantics: the value never exceeds cap.
            return Task.FromResult(Math.Min(CountToReturn, cap));
        }

        public Task ReplaceForProfileAsync(long profileId, long userId, IEnumerable<string> serviceCategoryCodes, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<bool> HasAnyForProfileAsync(long profileId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<ProviderAreaRow>> GetProvidersForAreaAsync(string cityCode, string? serviceCategoryCode, int take, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private static GetProviderAreaAvailabilityQueryHandler Handler(FakeRepo repo, int limitedMin = 1, int availableMin = 3)
        => new(repo, Options.Create(new AvailabilityThresholdsOptions { LimitedMin = limitedMin, AvailableMin = availableMin }));

    [Theory]
    [InlineData(0, "none")]
    [InlineData(1, "limited")]
    [InlineData(2, "limited")]
    [InlineData(3, "available")]
    public async Task Buckets_count_by_config_thresholds(int count, string expected)
    {
        var repo = new FakeRepo { CountToReturn = count };
        var dto = await Handler(repo).Handle(
            new GetProviderAreaAvailabilityQuery { CityCode = "34", CategoryCode = "MOTOR_MAINTENANCE" }, default);

        dto!.Availability.Should().Be(expected);
        dto.CityCode.Should().Be("34");
        dto.CategoryCode.Should().Be("MOTOR_MAINTENANCE");
    }

    [Fact]
    public async Task Fetches_at_most_available_min_rows_never_a_real_count()
    {
        var repo = new FakeRepo { CountToReturn = 999 };
        await Handler(repo, limitedMin: 1, availableMin: 3).Handle(
            new GetProviderAreaAvailabilityQuery { CityCode = "34", CategoryCode = "MOTOR_MAINTENANCE" }, default);

        repo.LastCap.Should().Be(3, "the cap bounds the query to the highest threshold — no full count is materialized");
    }

    [Fact]
    public async Task Thresholds_are_config_driven_retuning_changes_the_verdict()
    {
        var repo = new FakeRepo { CountToReturn = 2 };

        (await Handler(repo, limitedMin: 1, availableMin: 3).Handle(Q(), default))!.Availability.Should().Be("limited");
        (await Handler(repo, limitedMin: 1, availableMin: 2).Handle(Q(), default))!.Availability.Should().Be("available");
        (await Handler(repo, limitedMin: 3, availableMin: 5).Handle(Q(), default))!.Availability.Should().Be("none");

        static GetProviderAreaAvailabilityQuery Q() => new() { CityCode = "34", CategoryCode = "MOTOR_MAINTENANCE" };
    }

    [Fact]
    public async Task Empty_city_is_none_without_touching_the_repo()
    {
        var repo = new FakeRepo { CountToReturn = 99 };
        var dto = await Handler(repo).Handle(new GetProviderAreaAvailabilityQuery { CityCode = "  ", CategoryCode = "X" }, default);

        dto!.Availability.Should().Be("none");
        repo.LastCap.Should().Be(-1, "no query runs for an empty city");
    }
}
