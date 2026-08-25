using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Faz 28.8 — derin bağlantı doğrulama matrisi: göreli ok, allowlist https ok; javascript:/data:/dış host/"//" reddedilir.</summary>
public sealed class DeepLinkValidatorTests
{
    private static readonly string[] Allowlist = { "app.inktavia.com" };
    private static readonly string[] Empty = System.Array.Empty<string>();

    [Theory]
    [InlineData("/service-requests/9")]
    [InlineData("/")]
    [InlineData("/a/b/c?x=1")]
    public void Relative_paths_are_accepted(string deepLink)
    {
        var act = () => DeepLinkValidator.Validate(deepLink, Empty, "TPL");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_deeplink_is_accepted(string? deepLink)
    {
        var act = () => DeepLinkValidator.Validate(deepLink, Empty, "TPL");
        act.Should().NotThrow();
    }

    [Fact]
    public void Allowlisted_https_host_is_accepted()
    {
        var act = () => DeepLinkValidator.Validate("https://app.inktavia.com/sr/9", Allowlist, "TPL");
        act.Should().NotThrow();
    }

    [Fact]
    public void Http_allowlisted_host_is_accepted()
    {
        var act = () => DeepLinkValidator.Validate("http://app.inktavia.com/sr/9", Allowlist, "TPL");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("//evil.com/x")]                       // protokol-göreli
    [InlineData("https://evil.com/x")]                 // allowlist dışı host
    [InlineData("app://sr/9")]                         // özel şema
    [InlineData("ftp://app.inktavia.com/x")]           // http/https değil
    public void Dangerous_or_foreign_targets_are_rejected(string deepLink)
    {
        var act = () => DeepLinkValidator.Validate(deepLink, Allowlist, "TPL_X");
        act.Should().Throw<AizenBusinessException>().WithMessage("*TPL_X*");
    }

    [Fact]
    public void Empty_allowlist_rejects_absolute_https_even_if_well_formed()
    {
        var act = () => DeepLinkValidator.Validate("https://app.inktavia.com/x", Empty, "TPL");
        act.Should().Throw<AizenBusinessException>();
    }
}
