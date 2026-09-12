using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.Notification.Consumers.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.Notification.Application.UnitTests;

// The draft→submit portal path publishes ONLY ServiceRequestOfferSubmittedMessage, so this consumer is the sole
// notifier for real portal offers. Verify it fires provider OfferCreated + owner OfferReceived, and skips the owner
// when OwnerUserId == 0 (legacy publisher) so we never file a notification to user 0.
public sealed class ServiceRequestOfferSubmittedConsumerTests
{
    private const long ProviderProfileId = 5001;
    private const long OwnerUserId       = 9002;
    private const long OwnerProfileId    = 7003;

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly INotificationIdentityRemoteCall _identity = Substitute.For<INotificationIdentityRemoteCall>();

    private ServiceRequestOfferSubmittedConsumer BuildConsumer()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender);
        services.AddSingleton(_identity);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        // AizenBaseMessageConsumer resolves these in its ctor; unused by ExecuteCommitMessage.
        services.AddSingleton(Substitute.For<IRequestClient<AizenPrepareMessage<ServiceRequestOfferSubmittedMessage>>>());
        services.AddSingleton(Substitute.For<IRequestClient<AizenCommitMessage<ServiceRequestOfferSubmittedMessage>>>());
        services.AddSingleton(Substitute.For<IRequestClient<AizenRollbackMessage<ServiceRequestOfferSubmittedMessage>>>());
        return new ServiceRequestOfferSubmittedConsumer(services.BuildServiceProvider());
    }

    [Fact]
    public async Task ExecuteCommitMessage_notifies_provider_and_owner()
    {
        _identity.GetParticipantProfileIdByUserId(OwnerUserId).Returns(
            new AizenApiResponse<ParticipantProfileIdResult>(
                AizenResponseHeader.Success(),
                new ParticipantProfileIdResult { UserId = OwnerUserId, ProfileId = OwnerProfileId }));

        var consumer = BuildConsumer();
        var message = new ServiceRequestOfferSubmittedMessage
        {
            ServiceRequestId = 42, OfferId = 77, ProviderProfileId = ProviderProfileId,
            ProviderUserId = 1, OwnerUserId = OwnerUserId, TotalAmount = 100m, CurrencyCode = "TRY",
        };

        await consumer.ExecuteCommitMessage(message, CancellationToken.None);

        // Exactly two notifications: provider OfferCreated (filed under the provider profile) + owner OfferReceived
        // (filed under the resolved owner profile). InApp is the canonical inbox row (Email is the delivery mirror).
        _sender.Received(1).Send(
            Arg.Is<SendNotificationCommand>(c =>
                c.Channel == NotificationChannel.InApp && c.Type == NotificationType.OfferCreated &&
                c.RecipientUserId == ProviderProfileId),
            Arg.Any<CancellationToken>());
        _sender.Received(1).Send(
            Arg.Is<SendNotificationCommand>(c =>
                c.Channel == NotificationChannel.InApp && c.Type == NotificationType.OfferReceived &&
                c.RecipientUserId == OwnerProfileId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteCommitMessage_skips_owner_when_OwnerUserId_is_zero()
    {
        var consumer = BuildConsumer();
        var message = new ServiceRequestOfferSubmittedMessage
        {
            ServiceRequestId = 42, OfferId = 77, ProviderProfileId = ProviderProfileId,
            ProviderUserId = 1, OwnerUserId = 0, TotalAmount = 100m, CurrencyCode = "TRY",
        };

        await consumer.ExecuteCommitMessage(message, CancellationToken.None);

        // Provider still notified...
        _sender.Received(1).Send(
            Arg.Is<SendNotificationCommand>(c =>
                c.Channel == NotificationChannel.InApp && c.Type == NotificationType.OfferCreated),
            Arg.Any<CancellationToken>());
        // ...but the owner branch is skipped entirely: no OfferReceived, no profile resolve.
        _sender.DidNotReceive().Send(
            Arg.Is<SendNotificationCommand>(c => c.Type == NotificationType.OfferReceived),
            Arg.Any<CancellationToken>());
        await _identity.DidNotReceive().GetParticipantProfileIdByUserId(Arg.Any<long>());
    }
}
