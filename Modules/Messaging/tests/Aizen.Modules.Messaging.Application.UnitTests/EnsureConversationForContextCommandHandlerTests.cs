using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Application.Command.CreateConversation;
using Aizen.Modules.Messaging.Application.Command.EnsureConversation;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using FluentAssertions;

namespace Aizen.Modules.Messaging.Application.UnitTests;

/// <summary>
/// P0 spike — EnsureConversationForContext must be idempotent: a repeat call for the same (ContextType, ContextId)
/// returns the SAME conversation without creating a second one. (The DB additionally enforces this with a unique
/// index on (ContextType, ContextId); this test covers the handler's get-or-create logic.)
/// </summary>
public sealed class EnsureConversationForContextCommandHandlerTests
{
    private static EnsureConversationForContextCommand Cmd(long contextId) =>
        new(MessagingContextType.ServiceRequest, contextId, $"SR #{contextId}",
            new List<ConversationParticipantInput>
            {
                new(10008, "Owner", MessagingParticipantRole.Owner),
                new(100011, "Provider", MessagingParticipantRole.Provider),
            });

    [Fact]
    public async Task Ensure_IsIdempotent_SecondCallReturnsSameConversation_NoDuplicate()
    {
        var repo = new FakeConversationRepository();
        var handler = new EnsureConversationForContextCommandHandler(repo);

        var first = await handler.Handle(Cmd(9011), CancellationToken.None);
        var second = await handler.Handle(Cmd(9011), CancellationToken.None);

        first!.Created.Should().BeTrue();
        second!.Created.Should().BeFalse();
        second.ConversationId.Should().Be(first.ConversationId);

        repo.Store.Should().HaveCount(1);
        repo.Store[0].Participants.Should().HaveCount(2, "participants are added once, on creation only");
        repo.Store[0].ContextType.Should().Be(MessagingContextType.ServiceRequest);
        repo.Store[0].ContextId.Should().Be(9011);
    }

    [Fact]
    public async Task Ensure_DifferentContexts_CreateDistinctConversations()
    {
        var repo = new FakeConversationRepository();
        var handler = new EnsureConversationForContextCommandHandler(repo);

        var a = await handler.Handle(Cmd(9011), CancellationToken.None);
        var b = await handler.Handle(Cmd(9002), CancellationToken.None);

        a!.Created.Should().BeTrue();
        b!.Created.Should().BeTrue();
        b.ConversationId.Should().NotBe(a.ConversationId);
        repo.Store.Should().HaveCount(2);
    }

    // Minimal in-memory conversation repository simulating the (ContextType, ContextId) uniqueness + identity ids.
    private sealed class FakeConversationRepository : IConversationRepository
    {
        public readonly List<ConversationEntity> Store = new();
        private long _seq;

        public Task<ConversationEntity?> GetByContextAsync(
            MessagingContextType contextType, long contextId, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(
                c => c.ContextType == contextType && c.ContextId == contextId && !c.IsDeleted));

        public Task AddAsync(ConversationEntity entity, CancellationToken ct = default)
        {
            entity.Id = ++_seq; // simulate DB identity so participants + the response carry a real id
            Store.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(ConversationEntity entity) { /* no-op: entity already tracked in Store */ }

        public Task<IReadOnlyList<ConversationEntity>> GetListAsync(
            ConversationStatus? status, MessagingContextType? contextType, int skip, int take, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<ConversationEntity?> GetByIdAsync(long id, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<ConversationEntity?> GetByIdWithMessagesAsync(long id, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<int> CountAsync(ConversationStatus? status, MessagingContextType? contextType, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
