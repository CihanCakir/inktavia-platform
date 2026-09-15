using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Wave 4A — the email dispatcher embeds the notification's deep link in the body (the "no mail ever contained
/// a link" fix): absolute as-is, root-relative prefixed with WebBaseUrl, mobile scheme skipped, no duplication.</summary>
public sealed class EmailDeepLinkAppendTests
{
    private sealed class NoopRepo : INotificationRepository
    {
        public Task UpdateAsync(NotificationEntity e, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddAsync(NotificationEntity e, CancellationToken ct = default) => Task.CompletedTask;
        public Task<NotificationEntity?> GetByIdAsync(long id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(List<NotificationEntity> Items, int TotalCount)> GetHistoryPagedAsync(DateTimeOffset? f, DateTimeOffset? t, NotificationChannel? c, NotificationStatus? s, string? tc, long? r, long? cid, int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationEntity>> GetByRecipientAsync(long u, int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountByRecipientAsync(long u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> GetUnreadCountAsync(long u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> BulkMarkAsReadAsync(long u, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class CapturingSender : IEmailSender
    {
        public string? Body { get; private set; }
        public Task<string> SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
        { Body = htmlBody; return Task.FromResult("<ref@inktavia.com>"); }
    }

    private sealed class UnusedIdentity : INotificationIdentityRemoteCall
    {
        public Task<AizenApiResponse<List<ProviderForAreaResult>>> GetProvidersForArea(string c, string? cat = null, int take = 500) => throw new NotImplementedException();
        public Task<AizenApiResponse<List<long>>> GetAdminUserIds() => throw new NotImplementedException();
        public Task<AizenApiResponse<List<long>>> GetAllProviderProfileIds() => throw new NotImplementedException();
        public Task<AizenApiResponse<List<long>>> GetAllParticipantProfileIds() => throw new NotImplementedException();
        public Task<AizenApiResponse<ParticipantProfileIdResult>> GetParticipantProfileIdByUserId(long u) => throw new NotImplementedException();
        public Task<AizenApiResponse<ProfileContactEmailResult>> GetProfileContactEmail(long p) => throw new NotImplementedException();
        public Task<AizenApiResponse<ProfilePreferredLanguageResult>> GetProfilePreferredLanguage(long p) => throw new NotImplementedException();
        public Task<AizenApiResponse<ProfilePhoneNumberResult>> GetProfilePhoneNumber(long p) => throw new NotImplementedException();
    }

    private static async Task<string?> DispatchAsync(string? deepLink, string body, NotificationDeepLinkOptions opts)
    {
        var sender = new CapturingSender();
        var sut = new EmailNotificationDispatcher(sender, new NoopRepo(), new UnusedIdentity(), Options.Create(opts),
            NullLogger<EmailNotificationDispatcher>.Instance);
        var n = NotificationEntity.Create(1, NotificationType.ServiceRequestCreated, NotificationChannel.Email,
            "TPL", "Konu", body, metadataJson: "{\"recipientEmail\":\"t@inktavia.com\"}", deepLink: deepLink);
        await sut.DispatchAsync(n, CancellationToken.None);
        return sender.Body;
    }

    [Fact]
    public async Task Absolute_link_is_appended_as_is()
    {
        var body = await DispatchAsync("https://admin.inktavia.com/x", "<p>b</p>", new NotificationDeepLinkOptions());
        body.Should().Contain("https://admin.inktavia.com/x").And.Contain("<a href");
    }

    [Fact]
    public async Task Relative_link_is_absolutized_with_web_base()
    {
        var body = await DispatchAsync("/service-requests/9", "<p>b</p>",
            new NotificationDeepLinkOptions { WebBaseUrl = "https://web.inktavia.com" });
        body.Should().Contain("https://web.inktavia.com/service-requests/9");
    }

    [Fact]
    public async Task Relative_link_without_web_base_is_not_embedded()
    {
        var body = await DispatchAsync("/service-requests/9", "<p>b</p>", new NotificationDeepLinkOptions());
        body.Should().Be("<p>b</p>");
    }

    [Fact]
    public async Task Mobile_scheme_is_skipped()
    {
        var body = await DispatchAsync("inktavia-marine://service-requests/9", "<p>b</p>", new NotificationDeepLinkOptions());
        body.Should().Be("<p>b</p>");
    }

    [Fact]
    public async Task No_duplication_when_body_already_contains_the_link()
    {
        var body = await DispatchAsync("https://admin.inktavia.com/x", "<p>see https://admin.inktavia.com/x</p>", new NotificationDeepLinkOptions());
        System.Text.RegularExpressions.Regex.Matches(body!, "admin.inktavia.com/x").Count.Should().Be(1);
    }

    // ── owner bridge + per-audience shape inference ──
    [Fact]
    public async Task Owner_mobile_scheme_is_bridged_via_owner_link_base()
    {
        var body = await DispatchAsync("inktavia-marine://service-requests/57", "<p>b</p>",
            new NotificationDeepLinkOptions { OwnerLinkBaseUrl = "https://m.inktavia.com/link" });
        body.Should().Contain("https://m.inktavia.com/link/service-requests/57");
    }

    [Fact]
    public async Task Owner_relative_is_bridged_via_owner_link_base()
    {
        var body = await DispatchAsync("/service-requests/57", "<p>b</p>",
            new NotificationDeepLinkOptions { OwnerLinkBaseUrl = "https://m.inktavia.com/link", WebBaseUrl = "https://generic" });
        body.Should().Contain("https://m.inktavia.com/link/service-requests/57")
            .And.NotContain("https://generic");
    }

    [Fact]
    public async Task Provider_app_path_uses_provider_web_base()
    {
        var body = await DispatchAsync("/app/service-requests/57", "<p>b</p>",
            new NotificationDeepLinkOptions { ProviderWebBaseUrl = "https://provider.inktavia.com", OwnerLinkBaseUrl = "https://m.inktavia.com/link" });
        body.Should().Contain("https://provider.inktavia.com/app/service-requests/57")
            .And.NotContain("m.inktavia.com");
    }

    [Fact]
    public async Task Owner_falls_back_to_web_base_when_owner_link_base_unset()
    {
        var body = await DispatchAsync("/service-requests/57", "<p>b</p>",
            new NotificationDeepLinkOptions { WebBaseUrl = "https://web.inktavia.com" });
        body.Should().Contain("https://web.inktavia.com/service-requests/57");
    }
}
