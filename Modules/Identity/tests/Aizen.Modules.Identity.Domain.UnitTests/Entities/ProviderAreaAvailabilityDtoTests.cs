using System.Reflection;
using Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;
using FluentAssertions;

namespace Aizen.Modules.Identity.Domain.UnitTests.Entities;

/// <summary>
/// M2 — the coarse availability DTO must be structurally incapable of leaking provider counts or identities. It
/// carries ONLY the city, the optional category, and the bucketed verdict — no count/ids/names/scores/ranking.
/// </summary>
public sealed class ProviderAreaAvailabilityDtoTests
{
    [Fact]
    public void Carries_only_city_category_and_the_coarse_verdict()
    {
        var names = typeof(ProviderAreaAvailabilityDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        names.Should().BeEquivalentTo("CityCode", "CategoryCode", "Availability");

        foreach (var forbidden in new[]
                 {
                     "Count", "Total", "ProfileId", "UserId", "ProviderIds", "Providers", "Ids", "Score", "Rank", "Names",
                 })
            names.Should().NotContain(n => n.Contains(forbidden), $"'{forbidden}' must never surface on the coarse signal");
    }
}
