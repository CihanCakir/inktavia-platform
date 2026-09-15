using Aizen.Bff.Marine.Participant.Mobile.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests;

/// <summary>Owner deep-link bridge: allowlist accept/reject + open-redirect safety (scheme hardcoded, path validated).</summary>
public sealed class LinkBridgeControllerTests
{
    private static readonly LinkBridgeController Sut = new();

    [Theory]
    [InlineData("service-requests/57")]
    [InlineData("service-requests")]
    [InlineData("cargodry/kits/CD-ABC-123")]
    [InlineData("cargodry/catalog")]
    [InlineData("maintenance")]
    [InlineData("/service-requests/57")]   // leading slash tolerated
    public void Allowlisted_paths_return_html_with_the_hardcoded_scheme(string path)
    {
        var result = Sut.Bridge(path);
        var content = result.Should().BeOfType<ContentResult>().Subject;
        content.ContentType.Should().Contain("text/html");
        content.Content.Should().Contain("inktavia-marine://" + path.Trim('/'));
    }

    [Theory]
    [InlineData("https://evil.com")]                    // absolute → open-redirect attempt
    [InlineData("service-requests/57/../admin")]        // traversal
    [InlineData("cargodry/kits/CD ABC")]                // space
    [InlineData("service-requests/abc")]                // non-numeric id
    [InlineData("admin/users")]                         // not an owner shape
    [InlineData("service-requests/57?x=1")]             // query
    [InlineData("javascript:alert(1)")]                 // scheme injection
    [InlineData("")]                                    // empty
    public void Non_allowlisted_paths_404(string path)
    {
        Sut.Bridge(path).Should().BeOfType<NotFoundResult>();
    }
}
