using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Faz 28.7 — SMS dispatcher: telefon yok → sebepli Failed; stub ile mutlu yol → Sent + stub- ProviderRef.</summary>
public sealed class SmsNotificationDispatcherTests
{
    private readonly INotificationRepository _repo = Substitute.For<INotificationRepository>();
    private readonly INotificationIdentityRemoteCall _identity = Substitute.For<INotificationIdentityRemoteCall>();
    private static IOptions<SmsOptions> Opts() => Options.Create(new SmsOptions());

    private static NotificationEntity SmsRow(string body, string? metadataJson = null)
        => NotificationEntity.Create(42, NotificationType.AdminBroadcast, NotificationChannel.Sms,
            "TPL", title: string.Empty, body: body, metadataJson: metadataJson);

    [Fact]
    public async Task No_phone_marks_row_failed_with_reason_and_does_not_send()
    {
        var sender = Substitute.For<ISmsSender>();
        _identity.GetProfilePhoneNumber(Arg.Any<long>())
            .Returns(new AizenApiResponse<ProfilePhoneNumberResult> { Body = new ProfilePhoneNumberResult { ProfileId = 42, PhoneNumber = null } });

        var sut = new SmsNotificationDispatcher(sender, _repo, _identity, Opts(), NullLogger<SmsNotificationDispatcher>.Instance);
        var row = SmsRow("Merhaba");

        await sut.DispatchAsync(row, CancellationToken.None);

        row.Status.Should().Be(NotificationStatus.Failed);
        row.MetadataJson.Should().Contain("failureReason");
        row.MetadataJson.Should().Contain("telefon yok");
        await sender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Happy_path_via_stub_sends_and_marks_sent_with_stub_providerRef()
    {
        // Telefon MetadataJson'dan gelir (0'lı ulusal → +90...). Gerçek stub sender kullanılır.
        var stub = new LoggingSmsSenderStub(NullLogger<LoggingSmsSenderStub>.Instance);
        var sut = new SmsNotificationDispatcher(stub, _repo, _identity, Opts(), NullLogger<SmsNotificationDispatcher>.Instance);
        var row = SmsRow("Merhaba dünya", metadataJson: "{\"recipientPhone\":\"05551234567\"}");

        await sut.DispatchAsync(row, CancellationToken.None);

        row.Status.Should().Be(NotificationStatus.Sent);
        row.DeliveryProviderRef.Should().StartWith("stub-");
        // Telefon metadata'dan çözüldü → Identity'e gidilmedi.
        await _identity.DidNotReceive().GetProfilePhoneNumber(Arg.Any<long>());
    }

    [Fact]
    public async Task Send_failure_marks_row_failed_with_reason()
    {
        var sender = Substitute.For<ISmsSender>();
        sender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(SmsSendResult.Fail("gateway down"));

        var sut = new SmsNotificationDispatcher(sender, _repo, _identity, Opts(), NullLogger<SmsNotificationDispatcher>.Instance);
        var row = SmsRow("Merhaba", metadataJson: "{\"recipientPhone\":\"+905551234567\"}");

        await sut.DispatchAsync(row, CancellationToken.None);

        row.Status.Should().Be(NotificationStatus.Failed);
        row.MetadataJson.Should().Contain("gateway down");
    }
}
