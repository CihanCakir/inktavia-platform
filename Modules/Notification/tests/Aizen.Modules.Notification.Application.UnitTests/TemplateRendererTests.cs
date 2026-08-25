using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Exceptions;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// TEK strict renderer'ı kanıtlar: fallback zinciri (istenen → 'en' → ilk Published), içerik yokluğu, eksik
/// placeholder (strict), ve Email layout sarma. Ev tarzı: elle yazılmış fake repo'lar, gerçek TemplateInterpolator.
/// </summary>
public sealed class TemplateRendererTests
{
    private const long TemplateId = 1;
    private const string Code = "TPL_TEST";

    private sealed class FakeContentRepo : INotificationTemplateContentRepository
    {
        private readonly List<NotificationTemplateContentEntity> _all;
        public FakeContentRepo(params NotificationTemplateContentEntity[] all) => _all = all.ToList();

        public Task<List<NotificationTemplateContentEntity>> GetPublishedByTemplateCodeAndChannelAsync(
            string templateCode, NotificationChannel channel, CancellationToken ct = default)
            => Task.FromResult(_all
                .Where(c => c.Channel == channel && c.Status == TemplateContentStatus.Published)
                .ToList());

        public Task AddAsync(NotificationTemplateContentEntity e, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(NotificationTemplateContentEntity e, CancellationToken ct = default) => Task.CompletedTask;

        // Bu testlerde kullanılmayan admin yolları.
        public Task<List<NotificationTemplateContentEntity>> GetAllByTemplateIdAsync(long templateId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<NotificationTemplateContentEntity?> GetCurrentDraftAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<NotificationTemplateContentEntity?> GetCurrentPublishedAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationTemplateContentEntity>> GetVersionsAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> GetMaxVersionAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeLayoutRepo : IEmailLayoutRepository
    {
        private readonly EmailLayoutEntity? _layout;
        public FakeLayoutRepo(EmailLayoutEntity? layout = null) => _layout = layout;

        public Task<EmailLayoutEntity?> GetActiveByCodeAsync(string code, CancellationToken ct = default)
            => Task.FromResult(_layout is not null
                && string.Equals(_layout.Code, code, StringComparison.OrdinalIgnoreCase)
                && _layout.IsActive ? _layout : null);

        public Task AddAsync(EmailLayoutEntity e, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static NotificationTemplateContentEntity InApp(
        string locale, int version, string title, string body,
        TemplateContentStatus status = TemplateContentStatus.Published, string? deepLink = null)
        => NotificationTemplateContentEntity.Create(
            TemplateId, NotificationChannel.InApp, locale, version, status,
            titleTemplate: title, bodyTemplate: body, deepLinkTemplate: deepLink);

    private static NotificationTemplateContentEntity Email(
        string locale, int version, string subject, string html, string layoutCode = "DEFAULT")
        => NotificationTemplateContentEntity.Create(
            TemplateId, NotificationChannel.Email, locale, version, TemplateContentStatus.Published,
            subjectTemplate: subject, htmlTemplate: html, layoutCode: layoutCode);

    private static TemplateRenderer Sut(FakeContentRepo content, FakeLayoutRepo? layout = null)
        => new(content, layout ?? new FakeLayoutRepo(), new TemplateInterpolator());

    private static Dictionary<string, string> Vars(params (string k, string v)[] kv)
        => kv.ToDictionary(x => x.k, x => x.v);

    [Fact]
    public async Task Requested_locale_wins_over_en()
    {
        var sut = Sut(new FakeContentRepo(
            InApp("en", 1, "EN {{x}}", "en-body"),
            InApp("tr", 1, "TR {{x}}", "tr-body")));

        var r = await sut.RenderAsync(Code, NotificationChannel.InApp, "tr", Vars(("x", "1")));

        r.Title.Should().Be("TR 1");
        r.Body.Should().Be("tr-body");
    }

    [Fact]
    public async Task Falls_back_to_en_when_requested_locale_missing()
    {
        var sut = Sut(new FakeContentRepo(InApp("en", 1, "EN {{x}}", "b")));

        var r = await sut.RenderAsync(Code, NotificationChannel.InApp, "tr", Vars(("x", "1")));

        r.Title.Should().Be("EN 1");
    }

    [Fact]
    public async Task Falls_back_to_first_published_when_neither_requested_nor_en()
    {
        var sut = Sut(new FakeContentRepo(InApp("de", 1, "DE {{x}}", "b")));

        var r = await sut.RenderAsync(Code, NotificationChannel.InApp, "tr", Vars(("x", "1")));

        r.Title.Should().Be("DE 1");
    }

    [Fact]
    public async Task Highest_version_wins_within_a_locale()
    {
        var sut = Sut(new FakeContentRepo(
            InApp("en", 1, "v1", "b1"),
            InApp("en", 2, "v2", "b2")));

        var r = await sut.RenderAsync(Code, NotificationChannel.InApp, "en", Vars());

        r.Title.Should().Be("v2");
    }

    [Fact]
    public async Task No_published_content_throws_TemplateContentMissing()
    {
        // Yalnız Email içeriği var; InApp istenince zincir boş → hata.
        var sut = Sut(new FakeContentRepo(Email("en", 1, "s", "h")));

        var act = async () => await sut.RenderAsync(Code, NotificationChannel.InApp, "en", Vars());

        await act.Should().ThrowAsync<TemplateContentMissingException>();
    }

    [Fact]
    public async Task Draft_content_is_not_selected_and_throws_when_only_draft_exists()
    {
        var sut = Sut(new FakeContentRepo(
            InApp("en", 1, "draft", "b", TemplateContentStatus.Draft)));

        var act = async () => await sut.RenderAsync(Code, NotificationChannel.InApp, "en", Vars());

        await act.Should().ThrowAsync<TemplateContentMissingException>();
    }

    [Fact]
    public async Task Missing_placeholder_throws_listing_all_missing_keys()
    {
        var sut = Sut(new FakeContentRepo(InApp("en", 1, "Hi {{name}}", "{{a}} and {{b}}")));

        var act = async () => await sut.RenderAsync(Code, NotificationChannel.InApp, "en", Vars(("name", "Ada")));

        var ex = (await act.Should().ThrowAsync<TemplatePlaceholderMissingException>()).Which;
        ex.MissingKeys.Should().Contain(new[] { "a", "b" });
        ex.TemplateCode.Should().Be(Code);
    }

    [Fact]
    public async Task Email_html_is_wrapped_into_layout_shell_at_content_placeholder()
    {
        var layout = EmailLayoutEntity.Create("DEFAULT", "L", "<div>{{content}}</div>");
        var sut = Sut(new FakeContentRepo(Email("en", 1, "Subj {{x}}", "<p>{{x}}</p>")), new FakeLayoutRepo(layout));

        var r = await sut.RenderAsync(Code, NotificationChannel.Email, "en", Vars(("x", "Z")));

        r.Title.Should().Be("Subj Z");            // konu
        r.Body.Should().Be("<div><p>Z</p></div>"); // gövde layout kabuğuna sarılı
    }

    [Fact]
    public async Task Email_without_resolvable_layout_falls_back_to_unwrapped_html()
    {
        // Layout repo boş → sarma yapılmaz, güvenli geri düşüş: ham HTML.
        var sut = Sut(new FakeContentRepo(Email("en", 1, "S", "<p>{{x}}</p>")), new FakeLayoutRepo(null));

        var r = await sut.RenderAsync(Code, NotificationChannel.Email, "en", Vars(("x", "Q")));

        r.Body.Should().Be("<p>Q</p>");
    }

    [Fact]
    public async Task DeepLink_template_is_rendered_when_present()
    {
        // Faz 28.8: derin bağlantı güvenlik doğrulaması aktif → göreli yol kullanılıyor (özel şema "app://" reddedilir).
        var sut = Sut(new FakeContentRepo(InApp("en", 1, "T", "B", deepLink: "/sr/{{id}}")));

        var r = await sut.RenderAsync(Code, NotificationChannel.InApp, "en", Vars(("id", "9")));

        r.DeepLink.Should().Be("/sr/9");
    }
}
