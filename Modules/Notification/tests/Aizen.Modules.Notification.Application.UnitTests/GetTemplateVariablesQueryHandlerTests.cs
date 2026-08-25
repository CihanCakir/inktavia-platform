using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Query.GetTemplateVariables;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// GAP-2 değişken kataloğu: SR_CREATED_INAPP'in Type'ı (ServiceRequestCreated) hiçbir consumer tarafından
/// yayınlanmadığından katalogda karşılığı yoktur → canlı smoke'ta boş liste döndü. Handler artık katalog boşsa
/// placeholder'ları şablonun kendi içeriğinden ({{...}}) türetiyor; bu test bu düşüşü ve katalog yolunun bozulmadığını kanıtlar.
/// </summary>
public sealed class GetTemplateVariablesQueryHandlerTests
{
    private sealed class FakeTemplateRepo : INotificationTemplateRepository
    {
        private readonly NotificationTemplateEntity _template;
        public FakeTemplateRepo(NotificationTemplateEntity template) => _template = template;

        public Task<NotificationTemplateEntity?> GetByCodeAsync(string templateCode, CancellationToken ct = default)
            => Task.FromResult<NotificationTemplateEntity?>(_template);

        public Task<NotificationTemplateEntity?> GetActiveByTypeAndChannelAsync(NotificationType type, NotificationChannel channel, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationTemplateEntity>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(List<NotificationTemplateEntity> Items, int TotalCount)> GetPagedAsync(NotificationChannel? channel, string? locale, TemplateContentStatus? status, bool? enabled, string? search, int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(NotificationTemplateEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(NotificationTemplateEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeContentRepo : INotificationTemplateContentRepository
    {
        private readonly List<NotificationTemplateContentEntity> _all;
        public bool GetAllCalled { get; private set; }
        public FakeContentRepo(params NotificationTemplateContentEntity[] all) => _all = all.ToList();

        public Task<List<NotificationTemplateContentEntity>> GetAllByTemplateIdAsync(long templateId, CancellationToken ct = default)
        {
            GetAllCalled = true;
            return Task.FromResult(_all);
        }

        public Task<List<NotificationTemplateContentEntity>> GetPublishedByTemplateCodeAndChannelAsync(string templateCode, NotificationChannel channel, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<NotificationTemplateContentEntity?> GetCurrentDraftAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<NotificationTemplateContentEntity?> GetCurrentPublishedAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationTemplateContentEntity>> GetVersionsAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> GetMaxVersionAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(NotificationTemplateContentEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(NotificationTemplateContentEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static GetTemplateVariablesQueryHandler Sut(
        NotificationTemplateEntity template, FakeContentRepo content)
        => new(new FakeTemplateRepo(template), content, new TemplateInterpolator());

    // SR_CREATED_INAPP mirror: Type=ServiceRequestCreated (hiçbir consumer emit etmez) → katalog boş → içerikten düşüş.
    [Fact]
    public async Task SrCreated_type_yields_requestCode_and_serviceName_via_content_fallback()
    {
        var template = NotificationTemplateEntity.Create(
            "SR_CREATED_INAPP", "Service Request Created (In-App)",
            NotificationType.ServiceRequestCreated, NotificationChannel.InApp,
            "New Service Request: {{requestCode}}",
            "A new service request {{requestCode}} has been created for {{serviceName}}.");

        // Seed, template başlık/gövdesini içerik satırına kopyalar → placeholder'lar burada.
        var content = new FakeContentRepo(NotificationTemplateContentEntity.Create(
            templateId: 1, NotificationChannel.InApp, "en", version: 1, TemplateContentStatus.Published,
            titleTemplate: "New Service Request: {{requestCode}}",
            bodyTemplate: "A new service request {{requestCode}} has been created for {{serviceName}}."));

        var r = await Sut(template, content).Handle(
            new GetTemplateVariablesQuery { Code = "SR_CREATED_INAPP" }, CancellationToken.None);

        r.Type.Should().Be(NotificationType.ServiceRequestCreated);
        r.Placeholders.Should().Contain(new[] { "requestCode", "serviceName" });
        content.GetAllCalled.Should().BeTrue();     // düşüş yolu gerçekten kullanıldı
    }

    // Arşiv satırları düşüşe dahil edilmez; yalnız Draft/Published taranır.
    [Fact]
    public async Task Content_fallback_ignores_archived_rows()
    {
        var template = NotificationTemplateEntity.Create(
            "SR_CREATED_INAPP", "X", NotificationType.ServiceRequestCreated, NotificationChannel.InApp, "t", "b");

        var content = new FakeContentRepo(
            NotificationTemplateContentEntity.Create(1, NotificationChannel.InApp, "en", 2, TemplateContentStatus.Archived,
                titleTemplate: "{{staleKey}}"),
            NotificationTemplateContentEntity.Create(1, NotificationChannel.InApp, "en", 3, TemplateContentStatus.Draft,
                titleTemplate: "{{requestCode}}"));

        var r = await Sut(template, content).Handle(
            new GetTemplateVariablesQuery { Code = "SR_CREATED_INAPP" }, CancellationToken.None);

        r.Placeholders.Should().Contain("requestCode");
        r.Placeholders.Should().NotContain("staleKey");
    }

    // Katalog yolu bozulmamalı: Type katalogda varsa içeriğe hiç bakılmaz.
    [Fact]
    public async Task Catalog_path_used_when_type_is_known_without_touching_content()
    {
        var template = NotificationTemplateEntity.Create(
            "SR_PUBLISHED_INAPP", "Published", NotificationType.ServiceRequestPublished, NotificationChannel.InApp,
            "Talebiniz yayında: {{requestCode}}", "Talebiniz {{requestCode}} artık yayında.");

        var content = new FakeContentRepo(); // boş; çağrılırsa katalog yolu atlanmış demektir

        var r = await Sut(template, content).Handle(
            new GetTemplateVariablesQuery { Code = "SR_PUBLISHED_INAPP" }, CancellationToken.None);

        r.Placeholders.Should().BeEquivalentTo(new[] { "requestCode", "serviceName" });
        content.GetAllCalled.Should().BeFalse();    // katalog dolu → içeriğe düşülmedi
    }
}
