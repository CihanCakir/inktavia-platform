using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SaveTemplateContentDraft;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;
using NSubstitute;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Faz 28.8 — taslak-kaydetmede STATİK derin bağlantı doğrulanır; placeholder'lı olan render-time'a bırakılır.</summary>
public sealed class DraftSaveDeepLinkTests
{
    private static (SaveTemplateContentDraftCommandHandler Handler, INotificationTemplateContentRepository Content) Sut()
    {
        var templateRepo = Substitute.For<INotificationTemplateRepository>();
        templateRepo.GetByCodeAsync("TPL", Arg.Any<CancellationToken>())
            .Returns(NotificationTemplateEntity.Create("TPL", "n", NotificationType.AdminBroadcast, NotificationChannel.InApp, "t", "b"));
        var contentRepo = Substitute.For<INotificationTemplateContentRepository>();
        // Yeni draft yolu için: mevcut draft yok, max sürüm 0.
        contentRepo.GetCurrentDraftAsync(Arg.Any<long>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((NotificationTemplateContentEntity?)null);
        contentRepo.GetMaxVersionAsync(Arg.Any<long>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);
        return (new SaveTemplateContentDraftCommandHandler(templateRepo, contentRepo), contentRepo);
    }

    [Fact]
    public async Task Static_javascript_deeplink_is_rejected_at_save()
    {
        var (handler, content) = Sut();
        var cmd = new SaveTemplateContentDraftCommand
        {
            Code = "TPL", Channel = NotificationChannel.InApp, Locale = "tr",
            DeepLinkTemplate = "javascript:alert(1)",
        };

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>();
        await content.DidNotReceive().AddAsync(Arg.Any<NotificationTemplateContentEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deeplink_with_placeholder_is_deferred_to_render_and_saves()
    {
        var (handler, content) = Sut();
        var cmd = new SaveTemplateContentDraftCommand
        {
            Code = "TPL", Channel = NotificationChannel.InApp, Locale = "tr",
            // Placeholder içeriyor → save'de doğrulanmaz (render-time kontrolü uygular).
            DeepLinkTemplate = "{{scheme}}://x",
        };

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await content.Received(1).AddAsync(Arg.Any<NotificationTemplateContentEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Static_relative_deeplink_is_accepted_at_save()
    {
        var (handler, content) = Sut();
        var cmd = new SaveTemplateContentDraftCommand
        {
            Code = "TPL", Channel = NotificationChannel.InApp, Locale = "tr",
            DeepLinkTemplate = "/service-requests/9",
        };

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await content.Received(1).AddAsync(Arg.Any<NotificationTemplateContentEntity>(), Arg.Any<CancellationToken>());
    }
}
