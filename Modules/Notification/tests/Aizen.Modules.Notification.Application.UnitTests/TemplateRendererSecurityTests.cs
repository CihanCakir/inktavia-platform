using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// Faz 28.8 — XSS sertleştirmesi: Email gövdesinde değişken DEĞERLERİ HTML-encode edilir (şablon markup'ı değil,
/// konu değil); Sms/InApp gövdeleri encode EDİLMEZ (düz metin).
/// </summary>
public sealed class TemplateRendererSecurityTests
{
    private const string Code = "TPL_SEC";

    private sealed class FakeContentRepo : INotificationTemplateContentRepository
    {
        private readonly NotificationTemplateContentEntity _content;
        public FakeContentRepo(NotificationTemplateContentEntity content) => _content = content;

        public Task<List<NotificationTemplateContentEntity>> GetPublishedByTemplateCodeAndChannelAsync(
            string templateCode, NotificationChannel channel, CancellationToken ct = default)
            => Task.FromResult(_content.Channel == channel
                ? new List<NotificationTemplateContentEntity> { _content }
                : new List<NotificationTemplateContentEntity>());

        public Task AddAsync(NotificationTemplateContentEntity e, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(NotificationTemplateContentEntity e, CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<NotificationTemplateContentEntity>> GetAllByTemplateIdAsync(long templateId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<NotificationTemplateContentEntity?> GetCurrentDraftAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<NotificationTemplateContentEntity?> GetCurrentPublishedAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationTemplateContentEntity>> GetVersionsAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> GetMaxVersionAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class NullLayoutRepo : IEmailLayoutRepository
    {
        public Task<EmailLayoutEntity?> GetActiveByCodeAsync(string code, CancellationToken ct = default) => Task.FromResult<EmailLayoutEntity?>(null);
        public Task AddAsync(EmailLayoutEntity e, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static TemplateRenderer Sut(NotificationTemplateContentEntity content)
        => new(new FakeContentRepo(content), new NullLayoutRepo(), new TemplateInterpolator());

    private static Dictionary<string, string> Vars(string msg) => new() { ["msg"] = msg };

    [Fact]
    public async Task Email_html_encodes_variable_values_but_not_subject_or_template_markup()
    {
        var content = NotificationTemplateContentEntity.Create(
            templateId: 1, NotificationChannel.Email, "en", 1, TemplateContentStatus.Published,
            subjectTemplate: "{{msg}}", htmlTemplate: "<p>{{msg}}</p>");

        var r = await Sut(content).RenderAsync(Code, NotificationChannel.Email, "en", Vars("<script>alert(1)</script>"));

        // Gövde: değiş DEĞERİ encode'lu; şablonun kendi <p> markup'ı korunur; ham <script> ASLA yok.
        r.Body.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
        r.Body.Should().Contain("<p>");
        r.Body.Should().NotContain("<script>");
        // Konu düz metin → ham değer (encode edilmez).
        r.Title.Should().Be("<script>alert(1)</script>");
    }

    [Fact]
    public async Task InApp_body_is_not_html_encoded()
    {
        var content = NotificationTemplateContentEntity.Create(
            templateId: 1, NotificationChannel.InApp, "en", 1, TemplateContentStatus.Published,
            titleTemplate: "{{msg}}", bodyTemplate: "{{msg}}");

        var r = await Sut(content).RenderAsync(Code, NotificationChannel.InApp, "en", Vars("<b>x</b>"));

        r.Body.Should().Be("<b>x</b>");   // düz metin — encode yok
    }

    [Fact]
    public async Task Sms_body_is_not_html_encoded()
    {
        // Şablonun kendisinde '<' yok (SmsContentValidator geçer); değişken DEĞERİ ham enjekte edilir.
        var content = NotificationTemplateContentEntity.Create(
            templateId: 1, NotificationChannel.Sms, "en", 1, TemplateContentStatus.Published,
            smsTextTemplate: "{{msg}}");

        var r = await Sut(content).RenderAsync(Code, NotificationChannel.Sms, "en", Vars("<b>x</b>"));

        r.Body.Should().Be("<b>x</b>");   // düz metin — encode yok
    }
}
