using Aizen.Modules.Notification.Application.Command.SubmitContactMessage;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>M4 — the pure contact spam heuristics (honeypot, links, length, all-caps).</summary>
public sealed class ContactSpamScorerTests
{
    private static readonly ContactIntakeOptions Opt = new() { MaxLinks = 2 };

    [Fact]
    public void Honeypot_filled_is_max_score()
        => ContactSpamScorer.Score("hi", "a normal message", honeypot: "bot-was-here", Opt)
            .Should().Be(ContactSpamScorer.MaxScore);

    [Fact]
    public void Clean_message_scores_low()
        => ContactSpamScorer.Score("Question about mooring", "Hello, I'd like to ask about winter berths for my yacht.", null, Opt)
            .Should().BeLessThan(50);

    [Fact]
    public void Many_links_raise_the_score()
        => ContactSpamScorer.Score("deal", "buy http://a.com http://b.com https://c.com www.d.com now", null, Opt)
            .Should().BeGreaterThanOrEqualTo(40);

    [Fact]
    public void Too_short_and_all_caps_raise_the_score()
    {
        ContactSpamScorer.Score("x", "hi", null, Opt).Should().BeGreaterThanOrEqualTo(25);
        ContactSpamScorer.Score("SALE", "CLICK HERE NOW TO WIN A FREE PRIZE RIGHT AWAY TODAY", null, Opt)
            .Should().BeGreaterThanOrEqualTo(20);
    }
}
