using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Exceptions;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Aizen.Modules.Notification.Domain.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// Faz 28.6 — kampanya dağıtım servisi: locale gruplama (locale başına tek render), kısmi-başarı sayaç matematiği,
/// eksik-locale → Failed satır (asla yanlış dil), ScheduledAt-gelecek reddi.
/// </summary>
public sealed class CampaignDispatchServiceTests
{
    private readonly INotificationCampaignRepository _campaignRepo = Substitute.For<INotificationCampaignRepository>();
    private readonly ICampaignRecipientExpander      _expander     = Substitute.For<ICampaignRecipientExpander>();
    private readonly ILocaleResolver                 _locale       = Substitute.For<ILocaleResolver>();
    private readonly ITemplateRenderer               _renderer     = Substitute.For<ITemplateRenderer>();
    private readonly ITemplateInterpolator           _interp       = Substitute.For<ITemplateInterpolator>();
    private readonly INotificationTemplateRepository _templateRepo = Substitute.For<INotificationTemplateRepository>();
    private readonly INotificationRepository         _notifRepo    = Substitute.For<INotificationRepository>();
    private readonly INotificationDispatcher         _dispatcher   = Substitute.For<INotificationDispatcher>();

    private CampaignDispatchService Sut() => new(
        _campaignRepo, _expander, _locale, _renderer, _interp, _templateRepo, _notifRepo, _dispatcher,
        NullLogger<CampaignDispatchService>.Instance);

    private static NotificationCampaignEntity TemplateCampaign(
        string channelsCsv = "1", string selectedJson = "[1,2]", DateTimeOffset? scheduledAt = null)
        => NotificationCampaignEntity.Create(
            CampaignAudience.Provider, CampaignTargetMode.Selected, selectedJson,
            templateCode: "TPL", customContentJson: null, channelsCsv: channelsCsv,
            scheduledAt: scheduledAt, createdByUserId: 500);

    private void GivenTemplateExists()
        => _templateRepo.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(NotificationTemplateEntity.Create("TPL", "n", NotificationType.AdminBroadcast, NotificationChannel.InApp, "t", "b"));

    private void MarkDispatchSentFor(params long[] recipientIdsThatSucceed)
        => _dispatcher.When(x => x.DispatchAsync(Arg.Any<NotificationEntity>(), Arg.Any<CancellationToken>()))
            .Do(ci =>
            {
                var e = ci.Arg<NotificationEntity>();
                if (recipientIdsThatSucceed.Contains(e.RecipientUserId)) e.MarkAsSent();
                else e.MarkAsFailed();
            });

    [Fact]
    public async Task Two_recipients_two_locales_triggers_two_renders_grouped_by_locale()
    {
        var campaign = TemplateCampaign(selectedJson: "[1,2]", channelsCsv: "1");   // InApp
        _campaignRepo.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(campaign);
        GivenTemplateExists();
        _locale.ResolveAsync(1, Arg.Any<CancellationToken>()).Returns("tr");
        _locale.ResolveAsync(2, Arg.Any<CancellationToken>()).Returns("en");
        _renderer.RenderAsync(Arg.Any<string>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new RenderedContent("T", "B", null));
        MarkDispatchSentFor(1, 2);

        await Sut().DispatchAsync(0, CancellationToken.None);

        // İki locale × tek kanal → tam iki render (locale başına bir kez).
        await _renderer.Received(1).RenderAsync(Arg.Any<string>(), NotificationChannel.InApp, "tr",
            Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
        await _renderer.Received(1).RenderAsync(Arg.Any<string>(), NotificationChannel.InApp, "en",
            Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
        campaign.SentCount.Should().Be(2);
        campaign.Status.Should().Be(CampaignStatus.Completed);
    }

    [Fact]
    public async Task Partial_failure_counts_sent_and_failed_and_stays_completed()
    {
        var campaign = TemplateCampaign(selectedJson: "[1,2]", channelsCsv: "1");
        _campaignRepo.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(campaign);
        GivenTemplateExists();
        _locale.ResolveAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns("en");
        _renderer.RenderAsync(Arg.Any<string>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new RenderedContent("T", "B", null));
        MarkDispatchSentFor(1);   // yalnız alıcı 1 başarılı; 2 başarısız

        await Sut().DispatchAsync(0, CancellationToken.None);

        campaign.SentCount.Should().Be(1);
        campaign.FailedCount.Should().Be(1);
        campaign.Status.Should().Be(CampaignStatus.Completed);   // kısmi başarı Completed
    }

    [Fact]
    public async Task Missing_locale_content_creates_failed_row_and_never_dispatches()
    {
        var campaign = TemplateCampaign(selectedJson: "[1]", channelsCsv: "1");
        _campaignRepo.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(campaign);
        GivenTemplateExists();
        _locale.ResolveAsync(1, Arg.Any<CancellationToken>()).Returns("tr");
        _renderer.RenderAsync(Arg.Any<string>(), Arg.Any<NotificationChannel>(), "tr",
            Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns<RenderedContent>(_ => throw new TemplateContentMissingException("TPL", NotificationChannel.InApp, "tr"));

        var added = new List<NotificationEntity>();
        _notifRepo.When(x => x.AddAsync(Arg.Any<NotificationEntity>(), Arg.Any<CancellationToken>()))
            .Do(ci => added.Add(ci.Arg<NotificationEntity>()));

        await Sut().DispatchAsync(0, CancellationToken.None);

        var row = added.Should().ContainSingle().Subject;
        row.Status.Should().Be(NotificationStatus.Failed);
        row.Body.Should().Contain("locale içerik yok");         // insana okunur sebep gövdede
        row.MetadataJson.Should().Contain("reason");            // sebep MetadataJson'da da taşınır (JSON unicode-escape'li)
        row.CampaignId.Should().Be(campaign.Id);
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<NotificationEntity>(), Arg.Any<CancellationToken>());
        campaign.FailedCount.Should().Be(1);
        campaign.SentCount.Should().Be(0);
        campaign.Status.Should().Be(CampaignStatus.Failed);   // her satır başarısız → Failed
    }

    [Fact]
    public async Task Future_scheduledAt_is_stored_and_refused_without_dispatch()
    {
        var future = DateTimeOffset.UtcNow.AddYears(5);
        var campaign = TemplateCampaign(scheduledAt: future);
        _campaignRepo.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(campaign);

        await Sut().DispatchAsync(0, CancellationToken.None);

        campaign.Status.Should().Be(CampaignStatus.Queued);   // store-and-refuse: dokunulmaz
        await _campaignRepo.DidNotReceive().UpdateAsync(Arg.Any<NotificationCampaignEntity>(), Arg.Any<CancellationToken>());
        await _expander.DidNotReceive().ExpandAsync(Arg.Any<CampaignAudience>(), Arg.Any<CancellationToken>());
        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default, default!, default!, default);
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<NotificationEntity>(), Arg.Any<CancellationToken>());
    }
}
