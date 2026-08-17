using Aizen.Modules.Notification.Application;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Application.Services.Firebase;
using FirebaseAdmin.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// BE-MO9a — the real FirebaseAdmin FCM sender + the dev-safe DI switch. These exercise the pure, credential-free
/// parts: the config predicate + DI selection (no "Firebase" config → the stub is used), the payload→FCM message
/// mapping (APNs alert/sound/mutable-content + thread-id + Android + Data), the ≤500 multicast chunking, and the
/// FCM-error → deactivate-token classification. The real <see cref="FcmSender"/> ctor (FirebaseApp init) is never
/// constructed here — it is only registered when real creds are present (live push is a manual smoke test).
/// </summary>
public sealed class FcmSenderMo9aTests
{
    private static IConfiguration Config(params (string Key, string Value)[] pairs)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)))
            .Build();

    private static Type? FcmImplType(IConfiguration config)
        => new ServiceCollection()
            .AddNotificationApplicationServices(config)
            .First(d => d.ServiceType == typeof(IFcmSender))
            .ImplementationType;

    // (1) no "Firebase" config → the stub is used; the module still wires up.
    [Fact]
    public void No_firebase_config_uses_the_stub()
    {
        FcmImplType(Config()).Should().Be<FcmSenderStub>();
        // A half-configured section (ProjectId only, no private key / client email) is NOT enough → still the stub.
        FcmImplType(Config(("PushNotification:Firebase:ProjectId", "demo-project")))
            .Should().Be<FcmSenderStub>();
    }

    // (1b) a fully-configured service account → the real FcmSender is registered (not constructed here).
    [Fact]
    public void Configured_firebase_registers_the_real_sender()
    {
        var config = Config(
            ("PushNotification:Firebase:ProjectId", "demo-project"),
            ("PushNotification:Firebase:PrivateKey", "-----BEGIN PRIVATE KEY-----\\nfake\\n-----END PRIVATE KEY-----\\n"),
            ("PushNotification:Firebase:ClientEmail", "svc@demo-project.iam.gserviceaccount.com"));

        FcmImplType(config).Should().Be<FcmSender>();
    }

    [Fact]
    public void IsConfigured_requires_projectid_privatekey_and_clientemail()
    {
        new PushFirebaseSettings().IsConfigured.Should().BeFalse();
        new PushFirebaseSettings { ProjectId = "p" }.IsConfigured.Should().BeFalse();
        new PushFirebaseSettings { ProjectId = "p", PrivateKey = "k" }.IsConfigured.Should().BeFalse();
        new PushFirebaseSettings { ProjectId = "p", PrivateKey = "k", ClientEmail = "e" }.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void ServiceAccountJson_is_snake_case_and_omits_nothing_required()
    {
        var json = new PushFirebaseSettings
        {
            ProjectId = "demo", PrivateKey = "KEY", ClientEmail = "e@demo.iam", ClientId = "123",
        }.ToServiceAccountJson();

        json.Should().Contain("\"project_id\":\"demo\"");
        json.Should().Contain("\"private_key\":\"KEY\"");
        json.Should().Contain("\"client_email\":\"e@demo.iam\"");
        json.Should().Contain("\"type\":\"service_account\"");
        json.Should().Contain("\"token_uri\":\"https://oauth2.googleapis.com/token\"");
    }

    // (2) our payload → a correct per-platform message: APNs alert/sound/mutable-content + Android + Data + deep-link.
    [Fact]
    public void CreateMessage_maps_all_platforms_and_data()
    {
        var dataJson = "{\"notificationId\":141,\"referenceType\":\"Dispute\",\"referenceId\":9011}";
        var msg = FcmMessageMapper.CreateMessage("tok-abc", "Başlık", "Gövde", dataJson);

        msg.Token.Should().Be("tok-abc");
        msg.Notification!.Title.Should().Be("Başlık");
        msg.Notification.Body.Should().Be("Gövde");

        // Data blob → string→string (the deep-link ref the app routes on).
        msg.Data.Should().Contain("notificationId", "141");
        msg.Data.Should().Contain("referenceType", "Dispute");
        msg.Data.Should().Contain("referenceId", "9011");

        // APNs: alert + sound + mutable-content + thread-id (= referenceId) + push-type header.
        msg.Apns!.Aps.Alert!.Title.Should().Be("Başlık");
        msg.Apns.Aps.Alert.Body.Should().Be("Gövde");
        msg.Apns.Aps.Sound.Should().Be("default");
        msg.Apns.Aps.MutableContent.Should().BeTrue();
        msg.Apns.Aps.ThreadId.Should().Be("9011");
        msg.Apns.Headers.Should().Contain("apns-push-type", "alert");

        // Android notification.
        msg.Android!.Notification!.Title.Should().Be("Başlık");
        msg.Android.Notification.Body.Should().Be("Gövde");
    }

    [Fact]
    public void ParseData_is_null_and_malformed_safe()
    {
        FcmMessageMapper.ParseData(null).Should().BeEmpty();
        FcmMessageMapper.ParseData("").Should().BeEmpty();
        FcmMessageMapper.ParseData("not-json").Should().BeEmpty();
        FcmMessageMapper.ParseData("[1,2,3]").Should().BeEmpty();               // not an object
        FcmMessageMapper.ParseData("{\"a\":\"b\",\"n\":5}").Should().HaveCount(2);
    }

    // (3) multicast chunks >500 tokens into ≤500 batches.
    [Fact]
    public void ChunkTokens_splits_into_500_batches()
    {
        var tokens = Enumerable.Range(0, 1201).Select(i => $"tok-{i}").ToList();

        var chunks = FcmMessageMapper.ChunkTokens(tokens);

        chunks.Should().HaveCount(3);
        chunks[0].Should().HaveCount(500);
        chunks[1].Should().HaveCount(500);
        chunks[2].Should().HaveCount(201);
        chunks.SelectMany(c => c).Should().BeEquivalentTo(tokens, "no token is dropped or duplicated");
    }

    // (4) an Unregistered / InvalidArgument / SenderIdMismatch result → the token is deactivated; other errors keep it.
    [Theory]
    [InlineData(MessagingErrorCode.Unregistered, true)]
    [InlineData(MessagingErrorCode.InvalidArgument, true)]
    [InlineData(MessagingErrorCode.SenderIdMismatch, true)]
    [InlineData(MessagingErrorCode.Internal, false)]
    [InlineData(MessagingErrorCode.Unavailable, false)]
    [InlineData(MessagingErrorCode.QuotaExceeded, false)]
    [InlineData(MessagingErrorCode.ThirdPartyAuthError, false)]
    public void Classifier_flags_dead_tokens_for_deactivation(MessagingErrorCode code, bool shouldDeactivate)
    {
        FcmErrorClassifier.IsTokenInvalid(code).Should().Be(shouldDeactivate);
    }

    [Fact]
    public void Classifier_maps_error_types()
    {
        FcmErrorClassifier.Classify(MessagingErrorCode.Unregistered).Should().Be(FcmErrorType.InvalidToken);
        FcmErrorClassifier.Classify(MessagingErrorCode.InvalidArgument).Should().Be(FcmErrorType.InvalidToken);
        FcmErrorClassifier.Classify(MessagingErrorCode.SenderIdMismatch).Should().Be(FcmErrorType.SenderMismatch);
        FcmErrorClassifier.Classify(MessagingErrorCode.ThirdPartyAuthError).Should().Be(FcmErrorType.InvalidAuthConfiguration);
        FcmErrorClassifier.Classify(MessagingErrorCode.Unavailable).Should().Be(FcmErrorType.ServiceUnavailable);
    }
}
