using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.Content.Application.UnitTests;

public sealed class LanguageValidatorTests
{
    private static LanguageValidator With(params string[]? supported)
    {
        var builder = new ConfigurationBuilder();
        if (supported is not null)
            builder.AddInMemoryCollection(supported.Select((v, i) =>
                new KeyValuePair<string, string?>($"Content:Languages:Supported:{i}", v)));
        return new LanguageValidator(builder.Build());
    }

    [Fact]
    public void Default_supports_tr_and_en_case_insensitively()
    {
        var v = With(); // no config → defaults
        FluentActions.Invoking(() => v.EnsureSupported("tr")).Should().NotThrow();
        FluentActions.Invoking(() => v.EnsureSupported("EN")).Should().NotThrow();
    }

    [Fact]
    public void Unknown_code_is_rejected()
        => FluentActions.Invoking(() => With().EnsureSupported("zz")).Should().Throw<AizenBusinessException>();

    [Fact]
    public void Config_overrides_the_supported_set()
    {
        var v = With("tr", "de");
        FluentActions.Invoking(() => v.EnsureSupported("de")).Should().NotThrow();
        FluentActions.Invoking(() => v.EnsureSupported("en")).Should().Throw<AizenBusinessException>();
    }
}
