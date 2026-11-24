using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.ConfirmNewsletterSubscription;
using Aizen.Modules.Notification.Application.Command.SubscribeNewsletter;
using Aizen.Modules.Notification.Application.Command.SubmitContactMessage;
using Aizen.Modules.Notification.Application.Command.UnsubscribeNewsletter;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// P1 — newsletter double opt-in handlers. Idempotent + enumeration-safe subscribe, single-use/expiry-checked confirm,
/// idempotent unsubscribe. Spam policy is REUSED from contact intake (ContactSpamScorer + ContactIntakeOptions).
/// </summary>
public sealed class NewsletterHandlerTests
{
    // ── In-memory repository double (paged/stats/export are covered by the InMemory-EF repository tests). ──────────
    private sealed class FakeRepo : INewsletterSubscriberRepository
    {
        public readonly List<NewsletterSubscriberEntity> Rows = new();
        public int RecentCount { get; set; }
        private long _seq;

        public Task AddAsync(NewsletterSubscriberEntity e, CancellationToken ct = default)
        {
            typeof(NewsletterSubscriberEntity).GetProperty("Id")!.SetValue(e, ++_seq);
            Rows.Add(e);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(NewsletterSubscriberEntity e, CancellationToken ct = default) => Task.CompletedTask;
        public Task<NewsletterSubscriberEntity?> GetByEmailAsync(string email, CancellationToken ct = default)
            => Task.FromResult(Rows.FirstOrDefault(x => x.Email == email));
        public Task<NewsletterSubscriberEntity?> GetByIdAsync(long id, CancellationToken ct = default)
            => Task.FromResult(Rows.FirstOrDefault(x => x.Id == id));
        public Task<NewsletterSubscriberEntity?> GetByConfirmationTokenAsync(string token, CancellationToken ct = default)
            => Task.FromResult(Rows.FirstOrDefault(x => x.ConfirmationToken == token));
        public Task<NewsletterSubscriberEntity?> GetByUnsubscribeTokenAsync(string token, CancellationToken ct = default)
            => Task.FromResult(Rows.FirstOrDefault(x => x.UnsubscribeToken == token));
        public Task<int> CountRecentByIpHashAsync(string ipHash, DateTimeOffset since, CancellationToken ct = default)
            => Task.FromResult(RecentCount);
        public Task<(List<NewsletterSubscriberEntity>, int)> GetPagedAsync(
            NewsletterSubscriberStatus? status, string? lang, string? search, DateTimeOffset? from, DateTimeOffset? to,
            int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Dictionary<NewsletterSubscriberStatus, int>> GetStatusCountsAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<int> CountCreatedSinceAsync(DateTimeOffset since, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<NewsletterSubscriberEntity>> GetConfirmedForExportAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeMailer : INewsletterMailer
    {
        public int ConfirmCount { get; private set; }
        public int WelcomeCount { get; private set; }
        public string? LastConfirmUrl { get; private set; }
        public string? LastUnsubscribeUrl { get; private set; }
        public Task SendConfirmationAsync(string email, string lang, string confirmUrl, string unsubUrl, CancellationToken ct)
        { ConfirmCount++; LastConfirmUrl = confirmUrl; LastUnsubscribeUrl = unsubUrl; return Task.CompletedTask; }
        public Task SendWelcomeAsync(string email, string lang, string unsubUrl, CancellationToken ct)
        { WelcomeCount++; LastUnsubscribeUrl = unsubUrl; return Task.CompletedTask; }
    }

    // Capturing logger — records every formatted message so tests can assert on log content (e.g. no e-mail leaks,
    // a fallback-locale warning is emitted).
    private sealed class ListLogger<T> : ILogger<T>
    {
        public readonly List<string> Messages = new();
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }

    // P4 — the two locales the public website publishes (see appsettings / docker-compose), plus a default base.
    private static NewsletterIntakeOptions LinkOptions(
        string baseUrl = "https://x.test", int ttl = 48, string fallback = "tr")
        => new()
        {
            ConfirmationTokenTtlHours = ttl,
            PublicSiteBaseUrl = baseUrl,
            FallbackLang = fallback,
            Links =
            {
                ["tr"] = new NewsletterLinkPaths { Confirm = "/tr/bulten/onay", Unsubscribe = "/tr/bulten/abonelikten-cik" },
                ["en"] = new NewsletterLinkPaths { Confirm = "/en/newsletter/confirm", Unsubscribe = "/en/newsletter/unsubscribe" },
            },
        };

    private static NewsletterLinkBuilder LinkBuilder(
        NewsletterIntakeOptions? options = null, ILogger<NewsletterLinkBuilder>? logger = null)
        => new(Options.Create(options ?? LinkOptions()), logger ?? NullLogger<NewsletterLinkBuilder>.Instance);

    private static SubscribeNewsletterCommandHandler SubHandler(
        FakeRepo repo, FakeMailer mailer, int threshold = 50,
        NewsletterIntakeOptions? options = null,
        INewsletterLinkBuilder? links = null,
        ILogger<SubscribeNewsletterCommandHandler>? logger = null)
    {
        options ??= LinkOptions(ttl: 48);
        return new(repo, mailer, links ?? LinkBuilder(options),
            Options.Create(new ContactIntakeOptions { SpamThreshold = threshold, IpHashSalt = "salt", RateMaxPerWindow = 3 }),
            Options.Create(options),
            logger ?? NullLogger<SubscribeNewsletterCommandHandler>.Instance);
    }

    private static ConfirmNewsletterSubscriptionCommandHandler ConfirmHandler(
        FakeRepo repo, FakeMailer mailer, INewsletterLinkBuilder? links = null)
        => new(repo, mailer, links ?? LinkBuilder(),
            NullLogger<ConfirmNewsletterSubscriptionCommandHandler>.Instance);

    private static UnsubscribeNewsletterCommandHandler UnsubHandler(FakeRepo repo)
        => new(repo, NullLogger<UnsubscribeNewsletterCommandHandler>.Instance);

    private static SubscribeNewsletterCommand Cmd(string email = "ada@example.com", string? ip = null)
        => new() { Email = email, Lang = "tr", SourcePage = "/tr", ClientIp = ip };

    // 1
    [Fact]
    public async Task New_address_creates_pending_row_with_token_and_one_confirm_mail()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();

        var resp = await SubHandler(repo, mailer).Handle(Cmd(), default);

        resp!.Accepted.Should().BeTrue();
        repo.Rows.Should().ContainSingle();
        repo.Rows[0].Status.Should().Be(NewsletterSubscriberStatus.Pending);
        repo.Rows[0].ConfirmationToken.Should().NotBeNullOrEmpty();
        mailer.ConfirmCount.Should().Be(1, "exactly one confirmation mail");
    }

    // 2
    [Fact]
    public async Task Subscribing_twice_while_pending_reissues_token_without_a_second_row()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();

        await SubHandler(repo, mailer).Handle(Cmd(), default);
        var token1 = repo.Rows[0].ConfirmationToken;

        await SubHandler(repo, mailer).Handle(Cmd(), default);

        repo.Rows.Should().ContainSingle("no second row for the same address");
        repo.Rows[0].ConfirmationToken.Should().NotBe(token1, "the token is re-issued");
    }

    // 3
    [Fact]
    public async Task Subscribing_already_confirmed_returns_success_creates_no_row_sends_no_mail()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();
        repo.Rows.Add(NewsletterSubscriberEntity.CreateSeed(
            "ada@example.com", "tr", NewsletterSubscriberStatus.Confirmed, DateTimeOffset.UtcNow));

        var resp = await SubHandler(repo, mailer).Handle(Cmd(), default);

        resp!.Accepted.Should().BeTrue("enumeration protection — always accepted");
        repo.Rows.Should().ContainSingle("no new row");
        mailer.ConfirmCount.Should().Be(0, "a confirmed address is never re-mailed");
    }

    // 4
    [Fact]
    public async Task Confirming_valid_token_moves_to_confirmed_clears_token_sets_confirmedAt()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();
        await SubHandler(repo, mailer).Handle(Cmd(), default);
        var token = repo.Rows[0].ConfirmationToken!;

        var resp = await ConfirmHandler(repo, mailer).Handle(new ConfirmNewsletterSubscriptionCommand { Token = token }, default);

        resp!.Confirmed.Should().BeTrue();
        repo.Rows[0].Status.Should().Be(NewsletterSubscriberStatus.Confirmed);
        repo.Rows[0].ConfirmationToken.Should().BeNull();
        repo.Rows[0].ConfirmedAt.Should().NotBeNull();
        mailer.WelcomeCount.Should().Be(1);
    }

    // 5
    [Fact]
    public async Task Confirming_same_token_twice_fails_the_second_time()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();
        await SubHandler(repo, mailer).Handle(Cmd(), default);
        var token = repo.Rows[0].ConfirmationToken!;

        (await ConfirmHandler(repo, mailer).Handle(new ConfirmNewsletterSubscriptionCommand { Token = token }, default))!
            .Confirmed.Should().BeTrue();
        (await ConfirmHandler(repo, mailer).Handle(new ConfirmNewsletterSubscriptionCommand { Token = token }, default))!
            .Confirmed.Should().BeFalse("single-use: the token was cleared on first confirm");
    }

    // 6
    [Fact]
    public async Task Confirming_expired_token_fails()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();
        // TTL of 0 hours → the token is already expired the instant it is issued.
        var handler = SubHandler(repo, mailer, options: LinkOptions(ttl: 0));
        await handler.Handle(Cmd(), default);
        var token = repo.Rows[0].ConfirmationToken!;

        var resp = await ConfirmHandler(repo, mailer).Handle(new ConfirmNewsletterSubscriptionCommand { Token = token }, default);

        resp!.Confirmed.Should().BeFalse();
        repo.Rows[0].Status.Should().Be(NewsletterSubscriberStatus.Pending);
    }

    // 7
    [Fact]
    public async Task Unsubscribe_is_idempotent_and_sets_unsubscribedAt()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();
        await SubHandler(repo, mailer).Handle(Cmd(), default);
        var token = repo.Rows[0].UnsubscribeToken;

        (await UnsubHandler(repo).Handle(new UnsubscribeNewsletterCommand { Token = token }, default))!
            .Unsubscribed.Should().BeTrue();
        repo.Rows[0].Status.Should().Be(NewsletterSubscriberStatus.Unsubscribed);
        var first = repo.Rows[0].UnsubscribedAt;
        first.Should().NotBeNull();

        (await UnsubHandler(repo).Handle(new UnsubscribeNewsletterCommand { Token = token }, default))!
            .Unsubscribed.Should().BeTrue("unsubscribing twice succeeds");
        repo.Rows[0].UnsubscribedAt.Should().Be(first, "idempotent — the timestamp does not move");
    }

    // 8
    [Fact]
    public async Task Unsubscribed_address_can_resubscribe_and_returns_to_pending()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();
        repo.Rows.Add(NewsletterSubscriberEntity.CreateSeed(
            "ada@example.com", "tr", NewsletterSubscriberStatus.Unsubscribed, DateTimeOffset.UtcNow));

        var resp = await SubHandler(repo, mailer).Handle(Cmd(), default);

        resp!.Accepted.Should().BeTrue();
        repo.Rows.Should().ContainSingle();
        repo.Rows[0].Status.Should().Be(NewsletterSubscriberStatus.Pending);
        repo.Rows[0].ConfirmationToken.Should().NotBeNullOrEmpty();
        mailer.ConfirmCount.Should().Be(1);
    }

    // 9
    [Fact]
    public async Task Email_is_normalised_before_the_uniqueness_check()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();

        await SubHandler(repo, mailer).Handle(Cmd(email: "  Ada@Example.COM  "), default);
        await SubHandler(repo, mailer).Handle(Cmd(email: "ada@example.com"), default);

        repo.Rows.Should().ContainSingle("case/whitespace variants are the same subscriber");
        repo.Rows[0].Email.Should().Be("ada@example.com");
    }

    // 12
    [Fact]
    public async Task Score_above_threshold_persists_but_sends_no_mail_like_contact_intake()
    {
        // Rate burst pushes the score over the threshold — same policy/outcome as contact intake.
        var repo = new FakeRepo { RecentCount = 5 }; var mailer = new FakeMailer();

        var resp = await SubHandler(repo, mailer).Handle(Cmd(ip: "203.0.113.9"), default);

        resp!.Accepted.Should().BeTrue();
        repo.Rows.Should().ContainSingle("the row is still persisted");
        repo.Rows[0].Status.Should().Be(NewsletterSubscriberStatus.Pending);
        repo.Rows[0].SpamScore.Should().BeGreaterThanOrEqualTo(50);
        mailer.ConfirmCount.Should().Be(0, "over the threshold → no confirmation mail (a bot learns nothing)");
    }

    // ── P4 — locale-correct, fail-closed e-mail links ───────────────────────────────────────────────────────────────

    // P4.1 — a tr subscriber's confirmation link uses the configured Turkish route segments.
    [Fact]
    public async Task Tr_subscriber_confirmation_url_uses_tr_path()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();

        await SubHandler(repo, mailer, options: LinkOptions(baseUrl: "https://site.test"))
            .Handle(Cmd(email: "ada@example.com"), default);   // Cmd() is Lang = "tr"

        var token = repo.Rows[0].ConfirmationToken!;
        mailer.LastConfirmUrl.Should().Be($"https://site.test/tr/bulten/onay?token={Uri.EscapeDataString(token)}");
    }

    // P4.2 — an en subscriber's confirmation link uses the configured English route segments.
    [Fact]
    public async Task En_subscriber_confirmation_url_uses_en_path()
    {
        var repo = new FakeRepo(); var mailer = new FakeMailer();

        var cmd = new SubscribeNewsletterCommand { Email = "ada@example.com", Lang = "en", SourcePage = "/en" };
        await SubHandler(repo, mailer, options: LinkOptions(baseUrl: "https://site.test")).Handle(cmd, default);

        var token = repo.Rows[0].ConfirmationToken!;
        mailer.LastConfirmUrl.Should().Be($"https://site.test/en/newsletter/confirm?token={Uri.EscapeDataString(token)}");
    }

    // P4.3 — an unknown locale falls back to FallbackLang and logs a warning naming the missing locale (never the e-mail).
    [Fact]
    public void Unknown_locale_falls_back_to_fallback_lang_and_logs_a_warning()
    {
        var logger = new ListLogger<NewsletterLinkBuilder>();
        var builder = LinkBuilder(LinkOptions(baseUrl: "https://site.test", fallback: "tr"), logger);

        var url = builder.BuildConfirmUrl("de", "tok123");

        url.Should().Be("https://site.test/tr/bulten/onay?token=tok123", "unknown 'de' falls back to 'tr'");
        logger.Messages.Should().Contain(m => m.Contains("de") && m.Contains("tr"),
            "a warning names the missing locale and the fallback");
        logger.Messages.Should().NotContain(m => m.Contains("@"), "a locale warning never carries an e-mail address");
    }

    // P4.4 — a token with URL-unsafe characters is percent-encoded in the link.
    [Fact]
    public void Token_with_unsafe_characters_is_percent_encoded()
    {
        var builder = LinkBuilder(LinkOptions(baseUrl: "https://site.test"));

        var url = builder.BuildConfirmUrl("tr", "a b/c?d&e=f+g");

        url.Should().Be("https://site.test/tr/bulten/onay?token=" + Uri.EscapeDataString("a b/c?d&e=f+g"));
        url.Should().NotContain(" ").And.NotContain("a b/c?d&e=f+g");
    }

    // P4.5 — the deployment fails to start (validation exception) when PublicSiteBaseUrl is absent — not a silent default.
    [Fact]
    public void Options_validation_fails_at_startup_when_base_url_is_absent()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // PublicSiteBaseUrl intentionally omitted.
                ["NewsletterIntake:FallbackLang"] = "tr",
                ["NewsletterIntake:Links:tr:Confirm"] = "/tr/bulten/onay",
                ["NewsletterIntake:Links:tr:Unsubscribe"] = "/tr/bulten/abonelikten-cik",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<NewsletterIntakeOptions>()
            .Bind(config.GetSection(NewsletterIntakeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        using var sp = services.BuildServiceProvider();

        Action resolve = () => _ = sp.GetRequiredService<IOptions<NewsletterIntakeOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("PublicSiteBaseUrl");
    }

    // P4.5b — validation also fails when the fallback locale has no Links entry (even if the base URL is present).
    [Fact]
    public void Options_validation_fails_when_fallback_locale_link_is_missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NewsletterIntake:PublicSiteBaseUrl"] = "https://site.test",
                ["NewsletterIntake:FallbackLang"] = "tr",
                // No Links:tr entry.
                ["NewsletterIntake:Links:en:Confirm"] = "/en/newsletter/confirm",
                ["NewsletterIntake:Links:en:Unsubscribe"] = "/en/newsletter/unsubscribe",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<NewsletterIntakeOptions>()
            .Bind(config.GetSection(NewsletterIntakeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        using var sp = services.BuildServiceProvider();

        Action resolve = () => _ = sp.GetRequiredService<IOptions<NewsletterIntakeOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("tr");
    }

    // P4.6 — no log line emitted by the subscribe path carries the e-mail address.
    [Fact]
    public async Task Subscribe_path_never_logs_the_email_address()
    {
        var repo = new FakeRepo { RecentCount = 5 }; var mailer = new FakeMailer();   // rate-burst → over threshold log too
        var handlerLog = new ListLogger<SubscribeNewsletterCommandHandler>();
        var builderLog = new ListLogger<NewsletterLinkBuilder>();
        var options = LinkOptions();

        // Below-threshold subscribe (sends mail → exercises the link-builder + mail-sent log line).
        var below = new FakeRepo(); var belowMailer = new FakeMailer();
        await SubHandler(below, belowMailer, options: options,
            links: LinkBuilder(options, builderLog),
            logger: new ListLogger<SubscribeNewsletterCommandHandler>())
            .Handle(Cmd(email: "ada@example.com", ip: "203.0.113.9"), default);

        // Over-threshold subscribe (persist-but-skip-notify → exercises the threshold log line).
        await SubHandler(repo, mailer, options: options, logger: handlerLog)
            .Handle(Cmd(email: "ada@example.com", ip: "203.0.113.9"), default);

        handlerLog.Messages.Should().NotContain(m => m.Contains("ada@example.com"));
        handlerLog.Messages.Should().NotContain(m => m.Contains("@"), "subscriber id + locale only — never the address");
        builderLog.Messages.Should().NotContain(m => m.Contains("@"));
    }
}
