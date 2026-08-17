using System.Reflection;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.Api.GenericApi;   // AizenGenericApi<>
using Aizen.Core.Starter.Api.Generic;             // AizenGenericBffApi<>
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aizen.Core.Hardening.UnitTests;

/// <summary>
/// HARDENING_GENERIC_CRUD_AND_SERVICE_TOKEN — proves the anonymous generic surface is closed and the internal
/// service-token (BffAssertion) is enforced fail-closed outside Development:
///   • both generic controllers now require [Authorize] and carry no [AllowAnonymous] (class or CRUD verb);
///   • outside Development an empty BffAssertion:SharedSecret fails startup (options validation throws);
///   • with the secret + allowlist set it boots; in Development an empty secret still boots (unchanged);
///   • the pre-existing secret-set-but-empty-allowlist impersonation guard still fires.
/// </summary>
public sealed class GenericSurfaceAndServiceTokenHardeningTests
{
    // ── Generic surface: no anonymous read/write/delete remains ──────────────────────────────────────────

    [Fact]
    public void GenericBffApi_requires_authorize_and_is_not_anonymous()
    {
        var t = typeof(AizenGenericBffApi<>);
        t.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Should().NotBeEmpty(
            "the generic BFF CRUD controller must require an authenticated caller");
        t.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Should().BeEmpty(
            "the anonymous generic CRUD surface must be closed");
    }

    [Theory]
    [InlineData("AddEntity")]
    [InlineData("UpdateEntity")]
    [InlineData("DeleteEntity")]
    [InlineData("GetEntity")]
    [InlineData("SearchEntityForList")]
    [InlineData("SearchEntityForPaged")]
    public void GenericBffApi_verbs_have_no_anonymous_override(string method)
    {
        var m = typeof(AizenGenericBffApi<>).GetMethod(method);
        m.Should().NotBeNull();
        m!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Should().BeEmpty(
            $"no anonymous {method} may remain on the generic surface");
    }

    [Fact]
    public void GenericModuleApi_requires_authorize_and_is_not_anonymous()
    {
        var t = typeof(AizenGenericApi<>);
        t.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Should().NotBeEmpty();
        t.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Should().BeEmpty();
    }

    // ── BffAssertion: fail-closed outside Development ────────────────────────────────────────────────────

    private static IOptions<AizenBffAssertionOptions> BuildAssertionOptions(
        string? environment, string? sharedSecret, params string[] allowedClientIds)
    {
        var dict = new Dictionary<string, string?>();
        if (environment is not null) dict["ASPNETCORE_ENVIRONMENT"] = environment;
        if (sharedSecret is not null) dict["BffAssertion:SharedSecret"] = sharedSecret;
        for (var i = 0; i < allowedClientIds.Length; i++)
            dict[$"BffAssertion:AllowedClientIds:{i}"] = allowedClientIds[i];

        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAizenInfoAccessor(config);
        return services.BuildServiceProvider().GetRequiredService<IOptions<AizenBffAssertionOptions>>();
    }

    [Fact]
    public void NonDev_empty_secret_fails_closed()
    {
        var opts = BuildAssertionOptions(environment: "Production", sharedSecret: null);

        var act = () => _ = opts.Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*required outside Development*");
    }

    [Fact]
    public void NonDev_with_secret_and_allowlist_boots()
    {
        var opts = BuildAssertionOptions("Production", "s3cr3t-value", "admin-panel-bff", "provider-portal-bff");

        AizenBffAssertionOptions value = null!;
        var act = () => value = opts.Value;

        act.Should().NotThrow();
        value.SharedSecret.Should().Be("s3cr3t-value");
        value.AllowedClientIds.Should().Contain("admin-panel-bff");
    }

    [Fact]
    public void Development_empty_secret_still_boots_feature_disabled()
    {
        var opts = BuildAssertionOptions(environment: "Development", sharedSecret: null);

        var act = () => _ = opts.Value;

        act.Should().NotThrow("Development with no secret keeps the assertion feature disabled — behaviour unchanged");
        opts.Value.SharedSecret.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Secret_set_but_empty_allowlist_still_fails_impersonation_guard()
    {
        // Existing guard (any environment): a secret with no allowlist would let any service token impersonate.
        var opts = BuildAssertionOptions(environment: "Development", sharedSecret: "x");

        var act = () => _ = opts.Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*AllowedClientIds is empty*");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("QA")]
    public void Any_non_development_environment_requires_the_secret(string environment)
    {
        // The enforcement is "not Development", not hardcoded to Production — any non-dev env fails closed.
        var opts = BuildAssertionOptions(environment, sharedSecret: null);

        var act = () => _ = opts.Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*required outside Development*");
    }
}
