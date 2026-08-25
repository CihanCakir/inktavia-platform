using System.Reflection;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.Notification.Application.Command.SubmitContactMessage;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// M4 — the contact intake handler: persist always, notify admins only below the spam threshold, and ALWAYS return
/// { accepted:true, ticketRef } (a spammer learns nothing). IP is hashed, never stored raw.
/// </summary>
public sealed class SubmitContactMessageHandlerTests
{
    private sealed class FakeRepo : IContactMessageRepository
    {
        public ContactMessageEntity? Saved { get; private set; }
        public int RecentCount { get; set; }
        public Task AddAsync(ContactMessageEntity e, CancellationToken ct = default) { Saved = e; return Task.CompletedTask; }
        public Task<int> CountRecentByIpHashAsync(string ipHash, DateTimeOffset since, CancellationToken ct = default)
            => Task.FromResult(RecentCount);
    }

    private sealed class FakeIdentity : INotificationIdentityRemoteCall
    {
        public List<long> Admins { get; set; } = new();
        public Task<AizenApiResponse<List<long>>> GetAdminUserIds()
            => Task.FromResult(new AizenApiResponse<List<long>> { Body = Admins });
        public Task<AizenApiResponse<List<long>>> GetAllProviderProfileIds() => throw new NotImplementedException();
        public Task<AizenApiResponse<List<long>>> GetAllParticipantProfileIds() => throw new NotImplementedException();
        public Task<AizenApiResponse<List<ProviderForAreaResult>>> GetProvidersForArea(string cityCode, string? categoryCode = null, int take = 500)
            => throw new NotImplementedException();
        public Task<AizenApiResponse<ParticipantProfileIdResult>> GetParticipantProfileIdByUserId(long userId)
            => throw new NotImplementedException();
        public Task<AizenApiResponse<ProfileContactEmailResult>> GetProfileContactEmail(long profileId)
            => throw new NotImplementedException();
        public Task<AizenApiResponse<ProfilePreferredLanguageResult>> GetProfilePreferredLanguage(long profileId)
            => throw new NotImplementedException();
        public Task<AizenApiResponse<ProfilePhoneNumberResult>> GetProfilePhoneNumber(long profileId)
            => throw new NotImplementedException();
    }

    private sealed class FakeSender : ISender
    {
        public List<SendNotificationCommand> Sent { get; } = new();
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        {
            if (request is SendNotificationCommand c) Sent.Add(c);
            return Task.FromResult<TResponse>(default!);
        }
        // MediatR void overload — the handler uses the IRequest<TResponse> overload, so this is a no-op.
        public Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest
            => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken ct = default) => Task.FromResult<object?>(null);
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default)
            => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private static SubmitContactMessageCommandHandler Handler(FakeRepo repo, FakeIdentity id, FakeSender sender, int threshold = 50)
        => new(repo, id, sender, Options.Create(new ContactIntakeOptions { SpamThreshold = threshold, IpHashSalt = "salt" }),
            NullLogger<SubmitContactMessageCommandHandler>.Instance);

    private static SubmitContactMessageCommand Cmd(string message = "Hello, I'd like to enquire about winter berths.", string? honeypot = null, string? ip = "203.0.113.9")
        => new() { Name = "Ada", Email = "ada@example.com", Subject = "Berths", Message = message, Honeypot = honeypot, ClientIp = ip };

    [Fact]
    public async Task Below_threshold_persists_notifies_admins_and_accepts()
    {
        var repo = new FakeRepo();
        var id = new FakeIdentity { Admins = new() { 1, 2, 3 } };
        var sender = new FakeSender();

        var resp = await Handler(repo, id, sender).Handle(Cmd(), default);

        resp!.Accepted.Should().BeTrue();
        resp.TicketRef.Should().StartWith("CT-");
        repo.Saved.Should().NotBeNull();
        repo.Saved!.Status.Should().Be(Abstraction.Enum.ContactMessageStatus.New);
        sender.Sent.Should().HaveCount(3, "one notification per admin below threshold");
        sender.Sent.Should().OnlyContain(c => c.Type == Abstraction.Enum.NotificationType.ContactReceived);
    }

    [Fact]
    public async Task Honeypot_persists_but_skips_notify_and_still_accepts()
    {
        var repo = new FakeRepo();
        var id = new FakeIdentity { Admins = new() { 1, 2 } };
        var sender = new FakeSender();

        var resp = await Handler(repo, id, sender).Handle(Cmd(honeypot: "i-am-a-bot"), default);

        resp!.Accepted.Should().BeTrue("a bot must not be able to tell it was flagged");
        resp.TicketRef.Should().StartWith("CT-");
        repo.Saved!.SpamScore.Should().Be(ContactSpamScorer.MaxScore);
        sender.Sent.Should().BeEmpty("above the threshold → admins not notified");
    }

    [Fact]
    public async Task Rate_burst_pushes_over_threshold_and_skips_notify()
    {
        var repo = new FakeRepo { RecentCount = 5 };   // ≥ RateMaxPerWindow (default 3)
        var id = new FakeIdentity { Admins = new() { 1 } };
        var sender = new FakeSender();

        var resp = await Handler(repo, id, sender).Handle(Cmd(), default);

        resp!.Accepted.Should().BeTrue();
        repo.Saved!.SpamScore.Should().BeGreaterThanOrEqualTo(50);
        sender.Sent.Should().BeEmpty("rate penalty pushed the score over the threshold");
    }

    [Fact]
    public async Task Ip_is_hashed_never_stored_raw()
    {
        var repo = new FakeRepo();
        var resp = await Handler(repo, new FakeIdentity(), new FakeSender()).Handle(Cmd(ip: "203.0.113.9"), default);

        resp!.Accepted.Should().BeTrue();
        repo.Saved!.IpHash.Should().NotBeNullOrEmpty();
        repo.Saved.IpHash.Should().NotContain("203.0.113.9", "the raw IP must never be persisted");
        repo.Saved.IpHash!.Length.Should().Be(64, "SHA-256 hex");
    }

    [Fact]
    public void Response_carries_only_accepted_and_ticketRef()
        => typeof(SubmitContactResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).Should().BeEquivalentTo("Accepted", "TicketRef");
}
