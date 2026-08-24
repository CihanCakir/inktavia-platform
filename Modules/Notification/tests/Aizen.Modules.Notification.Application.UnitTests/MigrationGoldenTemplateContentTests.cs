using System.Reflection;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Seed;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// GOLDEN: migration'ın içerik taşıması (DefaultTemplateContent.FromTemplate = migration SQL ile AYNI eşleme) bir
/// InApp template için render sonucunu, ESKİ interpolation davranışıyla BİREBİR aynı üretmelidir. Böylece "migration
/// sonrası üretim davranışı değişmedi" değişmezi kanıtlanır.
/// </summary>
public sealed class MigrationGoldenTemplateContentTests
{
    private sealed class SingleContentRepo : INotificationTemplateContentRepository
    {
        private readonly NotificationTemplateContentEntity _content;
        public SingleContentRepo(NotificationTemplateContentEntity content) => _content = content;

        public Task<List<NotificationTemplateContentEntity>> GetPublishedByTemplateCodeAndChannelAsync(
            string templateCode, NotificationChannel channel, CancellationToken ct = default)
            => Task.FromResult(new List<NotificationTemplateContentEntity> { _content });

        public Task AddAsync(NotificationTemplateContentEntity e, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(NotificationTemplateContentEntity e, CancellationToken ct = default) => Task.CompletedTask;

        // Bu testte kullanılmayan admin yolları.
        public Task<List<NotificationTemplateContentEntity>> GetAllByTemplateIdAsync(long templateId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<NotificationTemplateContentEntity?> GetCurrentDraftAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<NotificationTemplateContentEntity?> GetCurrentPublishedAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationTemplateContentEntity>> GetVersionsAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> GetMaxVersionAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class EmptyLayoutRepo : IEmailLayoutRepository
    {
        public Task<EmailLayoutEntity?> GetActiveByCodeAsync(string code, CancellationToken ct = default)
            => Task.FromResult<EmailLayoutEntity?>(null);
        public Task AddAsync(EmailLayoutEntity e, CancellationToken ct = default) => Task.CompletedTask;
    }

    // Seed'in private static BuildTemplates()'ini yansımayla al (OwnerEventSetSeedMo9cTests ile aynı desen).
    private static IReadOnlyList<NotificationTemplateEntity> SeedTemplates()
    {
        var m = typeof(NotificationTemplateSeed).GetMethod("BuildTemplates",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return ((IEnumerable<NotificationTemplateEntity>)m.Invoke(null, null)!).ToList();
    }

    [Fact]
    public async Task Migrated_inapp_template_renders_identically_to_legacy_interpolation()
    {
        var template = SeedTemplates().First(t =>
            t.TemplateCode == "SR_CREATED_INAPP" && t.Channel == NotificationChannel.InApp);

        // Tüm placeholder'ları besle (strict render için gerekli).
        var vars = new Dictionary<string, string>
        {
            ["requestCode"] = "SR-1001",
            ["serviceName"] = "Hull Cleaning",
        };

        // Migration'ın ürettiği içerik satırı (aynı eşleme).
        var content = DefaultTemplateContent.FromTemplate(
            templateId: 1, template.Channel, template.TitleTemplate, template.BodyTemplate);

        var renderer = new TemplateRenderer(new SingleContentRepo(content), new EmptyLayoutRepo(), new TemplateInterpolator());
        var rendered = await renderer.RenderAsync(
            template.TemplateCode, template.Channel, "en", vars);

        // ESKİ davranış: aynı değişkenlerle lenient interpolation (tüm anahtarlar mevcut → strict ile birebir aynı).
        var legacy = new TemplateInterpolator();
        var expectedTitle = legacy.Interpolate(template.TitleTemplate, vars);
        var expectedBody  = legacy.Interpolate(template.BodyTemplate, vars);

        rendered.Title.Should().Be(expectedTitle);
        rendered.Body.Should().Be(expectedBody);
        rendered.Title.Should().Be("New Service Request: SR-1001");
        rendered.Body.Should().Be("A new service request SR-1001 has been created for Hull Cleaning.");
    }
}
