using Aizen.Modules.Content.Repository.Seed;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Content.Application.UnitTests;

public sealed class ContentDemoSeedTests
{
    private static ContentDemoSeed Seed(InMemoryItemRepo repo, bool enabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Seed:Demo"] = enabled ? "true" : "false" })
            .Build();
        return new ContentDemoSeed(repo, config, NullLogger<ContentDemoSeed>.Instance);
    }

    [Fact]
    public async Task Off_by_default_seeds_nothing()
    {
        var repo = new InMemoryItemRepo();
        await Seed(repo, enabled: false).SeedAsync();
        repo.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_seeds_demo_items_idempotently()
    {
        var repo = new InMemoryItemRepo();
        var seeder = Seed(repo, enabled: true);

        await seeder.SeedAsync();
        repo.Store.Should().HaveCount(3);
        repo.Store.Select(x => x.Type).Distinct().Should().HaveCountGreaterThan(1); // across types

        await seeder.SeedAsync(); // second run is a no-op
        repo.Store.Should().HaveCount(3);
    }
}
