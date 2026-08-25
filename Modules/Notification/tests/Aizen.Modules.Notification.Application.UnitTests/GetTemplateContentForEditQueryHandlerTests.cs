using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Query.GetTemplateContentForEdit;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// GAP-1 içerik-okuma sorgusu: editör düzenlemeden önce mevcut hücreyi görebilmeli. Kural: Draft öncelikli
/// (kaydetme akışı Draft üzerinden yürür), yoksa Published, ikisi de yoksa boş sonuç. Ev tarzı: elle yazılmış fake'ler.
/// </summary>
public sealed class GetTemplateContentForEditQueryHandlerTests
{
    private const string Code = "SR_CREATED_INAPP";

    private sealed class FakeTemplateRepo : INotificationTemplateRepository
    {
        private readonly NotificationTemplateEntity? _template;
        public FakeTemplateRepo(NotificationTemplateEntity? template) => _template = template;

        public Task<NotificationTemplateEntity?> GetByCodeAsync(string templateCode, CancellationToken ct = default)
            => Task.FromResult(_template);

        public Task<NotificationTemplateEntity?> GetActiveByTypeAndChannelAsync(NotificationType type, NotificationChannel channel, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationTemplateEntity>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(List<NotificationTemplateEntity> Items, int TotalCount)> GetPagedAsync(NotificationChannel? channel, string? locale, TemplateContentStatus? status, bool? enabled, string? search, int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(NotificationTemplateEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(NotificationTemplateEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeContentRepo : INotificationTemplateContentRepository
    {
        private readonly NotificationTemplateContentEntity? _draft;
        private readonly NotificationTemplateContentEntity? _published;
        public FakeContentRepo(NotificationTemplateContentEntity? draft, NotificationTemplateContentEntity? published)
        {
            _draft = draft;
            _published = published;
        }

        public Task<NotificationTemplateContentEntity?> GetCurrentDraftAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default)
            => Task.FromResult(_draft);
        public Task<NotificationTemplateContentEntity?> GetCurrentPublishedAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default)
            => Task.FromResult(_published);

        public Task<List<NotificationTemplateContentEntity>> GetPublishedByTemplateCodeAndChannelAsync(string templateCode, NotificationChannel channel, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationTemplateContentEntity>> GetAllByTemplateIdAsync(long templateId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationTemplateContentEntity>> GetVersionsAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> GetMaxVersionAsync(long templateId, NotificationChannel channel, string locale, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(NotificationTemplateContentEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(NotificationTemplateContentEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static NotificationTemplateEntity Template() => NotificationTemplateEntity.Create(
        Code, "SR Created", NotificationType.ServiceRequestCreated, NotificationChannel.InApp, "T {{x}}", "B {{x}}");

    private static NotificationTemplateContentEntity Content(int version, TemplateContentStatus status)
        => NotificationTemplateContentEntity.Create(
            templateId: 1, NotificationChannel.InApp, "en", version, status, titleTemplate: "T", bodyTemplate: "B");

    private static GetTemplateContentForEditQueryHandler Sut(FakeContentRepo content)
        => new(new FakeTemplateRepo(Template()), content);

    private static GetTemplateContentForEditQuery Query() =>
        new() { Code = Code, Channel = NotificationChannel.InApp, Locale = "en" };

    [Fact]
    public async Task Draft_is_preferred_over_published_when_both_exist()
    {
        var sut = Sut(new FakeContentRepo(
            draft: Content(3, TemplateContentStatus.Draft),
            published: Content(2, TemplateContentStatus.Published)));

        var r = await sut.Handle(Query(), CancellationToken.None);

        r.Found.Should().BeTrue();
        r.EditingSource.Should().Be(TemplateEditingSource.Draft);
        r.Content!.Version.Should().Be(3);
        r.Content.Status.Should().Be(TemplateContentStatus.Draft);
    }

    [Fact]
    public async Task Published_is_returned_when_no_draft_exists()
    {
        var sut = Sut(new FakeContentRepo(
            draft: null,
            published: Content(2, TemplateContentStatus.Published)));

        var r = await sut.Handle(Query(), CancellationToken.None);

        r.Found.Should().BeTrue();
        r.EditingSource.Should().Be(TemplateEditingSource.Published);
        r.Content!.Version.Should().Be(2);
        r.Content.Status.Should().Be(TemplateContentStatus.Published);
    }

    [Fact]
    public async Task Empty_result_when_neither_draft_nor_published_exists()
    {
        var sut = Sut(new FakeContentRepo(draft: null, published: null));

        var r = await sut.Handle(Query(), CancellationToken.None);

        r.Found.Should().BeFalse();
        r.EditingSource.Should().Be(TemplateEditingSource.None);
        r.Content.Should().BeNull();
    }
}
