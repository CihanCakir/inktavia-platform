using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SaveTemplateContentDraft;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;
using NSubstitute;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Faz 28.7 — SMS metninde '&lt;' reddi: hem saf doğrulayıcı hem taslak-kaydetme yolu.</summary>
public sealed class SmsHtmlRejectionTests
{
    [Fact]
    public void Validator_throws_when_sms_text_contains_lt()
    {
        var act = () => SmsContentValidator.EnsureNoHtml("Merhaba <b>x</b>", "TPL");
        act.Should().Throw<AizenBusinessException>().WithMessage("*TPL*");
    }

    [Fact]
    public void Validator_passes_plain_text_and_null()
    {
        SmsContentValidator.EnsureNoHtml("Merhaba dünya", "TPL");   // atmamalı
        SmsContentValidator.EnsureNoHtml(null, "TPL");
    }

    [Fact]
    public async Task Draft_save_rejects_sms_template_with_lt()
    {
        var templateRepo = Substitute.For<INotificationTemplateRepository>();
        templateRepo.GetByCodeAsync("TPL", Arg.Any<CancellationToken>())
            .Returns(NotificationTemplateEntity.Create("TPL", "n", NotificationType.AdminBroadcast, NotificationChannel.Sms, "t", "b"));
        var contentRepo = Substitute.For<INotificationTemplateContentRepository>();

        var sut = new SaveTemplateContentDraftCommandHandler(templateRepo, contentRepo);
        var cmd = new SaveTemplateContentDraftCommand
        {
            Code = "TPL", Channel = NotificationChannel.Sms, Locale = "tr",
            SmsTextTemplate = "Kod: {{code}} <script>",
        };

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>();
        // İçerik yazılmamalı.
        await contentRepo.DidNotReceive().AddAsync(Arg.Any<NotificationTemplateContentEntity>(), Arg.Any<CancellationToken>());
        await contentRepo.DidNotReceive().UpdateAsync(Arg.Any<NotificationTemplateContentEntity>(), Arg.Any<CancellationToken>());
    }
}
