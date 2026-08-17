using Aizen.Bff.Marine.Web.Application.Common.Options;
using Aizen.Bff.Marine.Web.Application.Common.Seo;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W5 — the W3.2 indexability verdict comes from the Options-backed <see cref="ISeoIndexabilityPolicy"/>, NOT from
/// hardcoded call-site <c>if</c>s. The key proof: flipping the same input's verdict by changing ONLY the Options
/// thresholds. The seam is swappable for managed data later without touching call sites.
/// </summary>
public sealed class SeoIndexabilityPolicyTests
{
    private sealed class MutableMonitor : IOptionsMonitor<MarineWebPublicOptions>
    {
        public MarineWebPublicOptions CurrentValue { get; set; }
        public MutableMonitor(MarineWebPublicOptions value) => CurrentValue = value;
        public MarineWebPublicOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<MarineWebPublicOptions, string?> listener) => null;
    }

    private static (OptionsSeoIndexabilityPolicy policy, MutableMonitor monitor) Build(MarineWebPublicOptions.SeoOptions seo)
    {
        var monitor = new MutableMonitor(new MarineWebPublicOptions { Seo = seo });
        return (new OptionsSeoIndexabilityPolicy(monitor), monitor);
    }

    [Fact]
    public void Unpublished_is_not_indexable_when_required()
    {
        var (policy, _) = Build(new() { RequirePublished = true });
        policy.Evaluate(new SeoEvaluationInput(IsPublished: false, "T", "D", 500))
            .Should().Match<SeoVerdict>(v => !v.Indexable && v.Reason == "not-published");
    }

    [Fact]
    public void Missing_title_and_thin_body_are_reported_with_reasons()
    {
        var (policy, _) = Build(new() { RequireTitle = true, MinBodyLength = 200 });

        policy.Evaluate(new SeoEvaluationInput(true, "   ", "D", 500))
            .Should().Match<SeoVerdict>(v => !v.Indexable && v.Reason == "missing-title");

        policy.Evaluate(new SeoEvaluationInput(true, "T", "D", 10))
            .Should().Match<SeoVerdict>(v => !v.Indexable && v.Reason.Contains("thin-content"));
    }

    [Fact]
    public void Full_detail_is_indexable_and_bodyless_projection_says_summary_level()
    {
        var (policy, _) = Build(new() { MinBodyLength = 200 });

        policy.Evaluate(new SeoEvaluationInput(true, "T", "D", 500))
            .Should().Match<SeoVerdict>(v => v.Indexable && v.Reason == "indexable");

        policy.Evaluate(new SeoEvaluationInput(true, "T", "D", BodyLength: null))
            .Should().Match<SeoVerdict>(v => v.Indexable && v.Reason.Contains("summary-level"));
    }

    [Fact]
    public void Verdict_changes_when_only_the_options_thresholds_change()
    {
        // Same input, three different Option settings → three different verdicts. Proves the threshold is managed
        // configuration, not compiled-in.
        var input = new SeoEvaluationInput(IsPublished: true, "T", Description: null, BodyLength: 120);

        var (policy, monitor) = Build(new() { RequireDescription = false, MinBodyLength = 100 });
        policy.Evaluate(input).Indexable.Should().BeTrue("desc not required, body 120 >= 100");

        monitor.CurrentValue = new MarineWebPublicOptions { Seo = new() { RequireDescription = true, MinBodyLength = 100 } };
        policy.Evaluate(input).Should().Match<SeoVerdict>(v => !v.Indexable && v.Reason == "missing-description");

        monitor.CurrentValue = new MarineWebPublicOptions { Seo = new() { RequireDescription = false, MinBodyLength = 200 } };
        policy.Evaluate(input).Should().Match<SeoVerdict>(v => !v.Indexable && v.Reason.Contains("thin-content"));
    }
}
