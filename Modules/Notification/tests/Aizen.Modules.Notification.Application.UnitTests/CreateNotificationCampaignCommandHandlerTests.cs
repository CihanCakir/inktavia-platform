using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Application.Command.CreateNotificationCampaign;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// Faz 28.6 — kampanya oluşturma doğrulaması: Sms kanalı reddi (Faz 7'ye kadar), kısmi-locale custom gönderim reddi
/// (sessiz yanlış-dil engeli), ve şablon yolu mutlu-yol (Queued + kuyruğa alınır).
/// </summary>
public sealed class CreateNotificationCampaignCommandHandlerTests
{
    private readonly INotificationCampaignRepository        _campaignRepo = Substitute.For<INotificationCampaignRepository>();
    private readonly INotificationTemplateRepository        _templateRepo = Substitute.For<INotificationTemplateRepository>();
    private readonly INotificationTemplateContentRepository _contentRepo  = Substitute.For<INotificationTemplateContentRepository>();
    private readonly IAizenInfoAccessor                     _info         = Substitute.For<IAizenInfoAccessor>();
    private readonly IAizenMessagePublisher                 _publisher    = Substitute.For<IAizenMessagePublisher>();

    public CreateNotificationCampaignCommandHandlerTests()
    {
        var userInfoAccessor = Substitute.For<IAizenUserInfoAccessor>();
        userInfoAccessor.UserInfo.Returns(new AizenUserInfo { UserId = 9 });
        _info.UserInfoAccessor.Returns(userInfoAccessor);
    }

    private CreateNotificationCampaignCommandHandler Sut() => new(
        _campaignRepo, _templateRepo, _contentRepo, _info, _publisher,
        NullLogger<CreateNotificationCampaignCommandHandler>.Instance);

    [Fact]
    public async Task Sms_channel_is_refused()
    {
        var cmd = new CreateNotificationCampaignCommand
        {
            Audience = CampaignAudience.Provider, TargetMode = CampaignTargetMode.All,
            TemplateCode = "TPL",
            Channels = new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.Sms },
        };

        var act = async () => await Sut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>();
        await _campaignRepo.DidNotReceive().AddAsync(Arg.Any<NotificationCampaignEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Partial_custom_locale_is_refused()
    {
        var cmd = new CreateNotificationCampaignCommand
        {
            Audience = CampaignAudience.Provider, TargetMode = CampaignTargetMode.All,
            Channels = new List<NotificationChannel> { NotificationChannel.InApp },
            // Yalnız 'tr' var, 'en' eksik → kısmi kapsama reddedilir.
            CustomContent = new Dictionary<string, CampaignLocaleContent>
            {
                ["tr"] = new() { Title = "Başlık", Body = "Gövde" },
            },
        };

        var act = async () => await Sut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>();
        await _campaignRepo.DidNotReceive().AddAsync(Arg.Any<NotificationCampaignEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Template_path_with_published_content_persists_queued_and_publishes()
    {
        _templateRepo.GetByCodeAsync("TPL", Arg.Any<CancellationToken>())
            .Returns(NotificationTemplateEntity.Create("TPL", "n", NotificationType.AdminBroadcast, NotificationChannel.InApp, "t", "b"));
        // En az bir desteklenen locale için yayınlanmış içerik var.
        _contentRepo.GetCurrentPublishedAsync(Arg.Any<long>(), NotificationChannel.InApp, "tr", Arg.Any<CancellationToken>())
            .Returns(NotificationTemplateContentEntity.Create(1, NotificationChannel.InApp, "tr", 1, TemplateContentStatus.Published, titleTemplate: "t", bodyTemplate: "b"));

        var cmd = new CreateNotificationCampaignCommand
        {
            Audience = CampaignAudience.Provider, TargetMode = CampaignTargetMode.All,
            TemplateCode = "TPL",
            Channels = new List<NotificationChannel> { NotificationChannel.InApp },
        };

        var result = await Sut().Handle(cmd, CancellationToken.None);

        result!.Status.Should().Be(CampaignStatus.Queued);
        await _campaignRepo.Received(1).AddAsync(Arg.Any<NotificationCampaignEntity>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(Arg.Any<NotificationCampaignQueuedMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Push_channel_is_now_accepted()
    {
        // Faz 7: Push artık kabul edilir (Sms hâlâ reddedilir). Push kanalı için yayınlanmış içerik olduğunu varsay.
        _templateRepo.GetByCodeAsync("TPL", Arg.Any<CancellationToken>())
            .Returns(NotificationTemplateEntity.Create("TPL", "n", NotificationType.AdminBroadcast, NotificationChannel.Push, "t", "b"));
        _contentRepo.GetCurrentPublishedAsync(Arg.Any<long>(), NotificationChannel.Push, "tr", Arg.Any<CancellationToken>())
            .Returns(NotificationTemplateContentEntity.Create(1, NotificationChannel.Push, "tr", 1, TemplateContentStatus.Published, titleTemplate: "t", bodyTemplate: "b"));

        var cmd = new CreateNotificationCampaignCommand
        {
            Audience = CampaignAudience.Provider, TargetMode = CampaignTargetMode.All,
            TemplateCode = "TPL",
            Channels = new List<NotificationChannel> { NotificationChannel.Push },
        };

        var result = await Sut().Handle(cmd, CancellationToken.None);

        result!.Status.Should().Be(CampaignStatus.Queued);
        await _campaignRepo.Received(1).AddAsync(Arg.Any<NotificationCampaignEntity>(), Arg.Any<CancellationToken>());
    }

    // ── DeepLinkPath doğrulaması ─────────────────────────────────────────────────────────────────────────

    private CreateNotificationCampaignCommand ValidCmdWith(string? deepLinkPath)
    {
        _templateRepo.GetByCodeAsync("TPL", Arg.Any<CancellationToken>())
            .Returns(NotificationTemplateEntity.Create("TPL", "n", NotificationType.AdminBroadcast, NotificationChannel.InApp, "t", "b"));
        _contentRepo.GetCurrentPublishedAsync(Arg.Any<long>(), NotificationChannel.InApp, "tr", Arg.Any<CancellationToken>())
            .Returns(NotificationTemplateContentEntity.Create(1, NotificationChannel.InApp, "tr", 1, TemplateContentStatus.Published, titleTemplate: "t", bodyTemplate: "b"));

        return new CreateNotificationCampaignCommand
        {
            Audience = CampaignAudience.Provider, TargetMode = CampaignTargetMode.All,
            TemplateCode = "TPL",
            Channels = new List<NotificationChannel> { NotificationChannel.InApp },
            DeepLinkPath = deepLinkPath,
        };
    }

    [Fact]
    public async Task DeepLinkPath_relative_path_is_accepted_and_persisted()
    {
        NotificationCampaignEntity? captured = null;
        await _campaignRepo.AddAsync(Arg.Do<NotificationCampaignEntity>(c => captured = c), Arg.Any<CancellationToken>());

        var result = await Sut().Handle(ValidCmdWith("/app/x"), CancellationToken.None);

        result!.Status.Should().Be(CampaignStatus.Queued);
        captured!.DeepLinkPath.Should().Be("/app/x");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeepLinkPath_null_or_empty_persists_null(string? deepLinkPath)
    {
        NotificationCampaignEntity? captured = null;
        await _campaignRepo.AddAsync(Arg.Do<NotificationCampaignEntity>(c => captured = c), Arg.Any<CancellationToken>());

        await Sut().Handle(ValidCmdWith(deepLinkPath), CancellationToken.None);

        captured!.DeepLinkPath.Should().BeNull();
    }

    [Theory]
    [InlineData("http://evil.com/x")]     // mutlak URL / şema
    [InlineData("https://evil.com")]      // mutlak URL / şema
    [InlineData("//evil.com/x")]          // protokol-göreli (açık-yönlendirme)
    [InlineData("../x")]                  // göreli ama '/' ile başlamıyor
    [InlineData("app/x")]                 // göreli ama '/' ile başlamıyor
    [InlineData("/\\evil.com")]           // ters eğik çizgi
    [InlineData("javascript:alert(1)")]   // özel şema
    public async Task DeepLinkPath_open_redirect_or_scheme_is_refused(string deepLinkPath)
    {
        var act = async () => await Sut().Handle(ValidCmdWith(deepLinkPath), CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>();
        await _campaignRepo.DidNotReceive().AddAsync(Arg.Any<NotificationCampaignEntity>(), Arg.Any<CancellationToken>());
    }
}
