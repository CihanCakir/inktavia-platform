using Aizen.Modules.Content.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Content.Application.UnitTests;

public sealed class SlugServiceTests
{
    [Theory]
    [InlineData("Deneme Başlık", "deneme-baslik")]
    [InlineData("  Hello, World!  ", "hello-world")]
    [InlineData("Ğüşöçı UPPER", "gusoci-upper")]
    [InlineData("multiple---dashes", "multiple-dashes")]
    public void Slugify_normalizes(string input, string expected)
        => new SlugService(new InMemoryItemRepo()).Slugify(input).Should().Be(expected);

    [Fact]
    public async Task GenerateUnique_suffixes_on_collision()
    {
        var repo = new InMemoryItemRepo();
        repo.Store.Add(TestData.Item(slug: "news"));
        repo.Store.Add(TestData.Item(slug: "news-2"));
        var svc = new SlugService(repo);

        (await svc.GenerateUniqueSlugAsync("News")).Should().Be("news-3");
    }

    [Fact]
    public async Task GenerateUnique_is_soft_delete_aware()
    {
        var repo = new InMemoryItemRepo();
        var deleted = TestData.Item(slug: "gone");
        deleted.IsDeleted = true;
        repo.Store.Add(deleted);
        var svc = new SlugService(repo);

        // A soft-deleted item releases its slug → no suffix.
        (await svc.GenerateUniqueSlugAsync("gone")).Should().Be("gone");
    }
}
