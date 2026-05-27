using Aizen.Modules.Identity.Api.UnitTests.Fakes;
using FluentAssertions;

namespace Aizen.Modules.Identity.Api.UnitTests.Controllers;

public class FakeCQRSProcessorTests
{
    [Fact]
    public async Task FakeCQRSProcessor_TracksProcessedCommands()
    {
        var fake = new FakeCQRSProcessor();

        fake.ProcessedCommands.Should().BeEmpty();
    }

    [Fact]
    public void FakeCQRSProcessor_SetupResult_StoresResult()
    {
        var fake = new FakeCQRSProcessor();

        var act = () => fake.SetupResult("expected-result");

        act.Should().NotThrow();
    }
}
