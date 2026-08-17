using Aizen.Bff.Marine.Web.Application.Common.Services;
using Aizen.Bff.Marine.Web.Application.Content.Command.AddWebContentComment;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W4 — participant engagement identity flow. The resolver populates the identity holder from the verified Keycloak
/// subject BEFORE the engagement call (so the auth handler attaches the assertion to Content /me), and the
/// resolver's OWN Identity call runs while the holder is still unset (no assertion → no recursion). Anonymous /
/// unlinked callers are rejected by the /me handler's identity gate.
/// </summary>
public sealed class EngagementIdentityTests
{
    private static OrganizerProfileDetailDto Profile(long userId, long profileId)
        => new() { UserId = userId, Id = profileId, FirstName = "A", LastName = "B", TaxpayerType = "Individual",
                   ApprovalStatus = "Approved", Status = "Active" };

    // ── real resolver ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolver_sets_holder_after_identity_call_no_recursion()
    {
        var context = new FakeWebParticipantContext(subject: "kc-sub-1");
        var holder = new WebIdentityHolder();
        var identity = new FakeIdentityRemoteCall(Profile(userId: 700, profileId: 900), holderToObserve: holder);
        var resolver = new WebParticipantProfileResolver(
            context, identity, holder, NullLogger<WebParticipantProfileResolver>.Instance);

        var resolution = await resolver.ResolveAsync();

        identity.WasCalled.Should().BeTrue();
        identity.HolderWasResolvedWhenCalled.Should().BeFalse(
            "the resolver's own Identity call must run BEFORE the holder is set → carries no assertion → no recursion");
        holder.Resolved.Should().BeTrue("after resolution the holder is populated → the NEXT (engagement) call is asserted");
        holder.UserId.Should().Be(700);
        holder.ProfileId.Should().Be(900);
        resolution.UserId.Should().Be(700);
    }

    [Fact]
    public async Task Resolver_returns_unresolved_when_no_subject_and_skips_identity()
    {
        var context = new FakeWebParticipantContext(subject: null);
        var holder = new WebIdentityHolder();
        var identity = new FakeIdentityRemoteCall(profile: null, holderToObserve: holder);
        var resolver = new WebParticipantProfileResolver(
            context, identity, holder, NullLogger<WebParticipantProfileResolver>.Instance);

        var resolution = await resolver.ResolveAsync();

        identity.WasCalled.Should().BeFalse("no subject ⇒ no Identity lookup");
        resolution.UserId.Should().BeNull();
        holder.Resolved.Should().BeFalse();
    }

    // ── /me handler identity gate ────────────────────────────────────────────────

    [Fact]
    public async Task AddComment_rejects_when_no_participant_identity()
    {
        var content = new FakeContentRemoteCall();   // AddComment throws if reached
        var handler = new AddWebContentCommentCommandHandler(
            new FakeWebParticipantProfileResolver(userId: null), content,
            NullLogger<AddWebContentCommentCommandHandler>.Instance);

        var act = () => handler.Handle(
            new AddWebContentCommentCommand { ContentId = "c1", Body = "Merhaba" }, default);

        await act.Should().ThrowAsync<AizenBusinessException>()
            .WithMessage("*participant identity*");
    }

    [Fact]
    public async Task AddComment_happy_path_maps_own_comment()
    {
        var content = new FakeContentRemoteCall
        {
            AddCommentResponse = Env.Ok(new ContentCommentDto
            {
                Id = "m1", ContentId = "c1", Body = "Merhaba", Status = ContentCommentStatus.Pending,
                AuthorUserId = 700, CreatedAt = DateTimeOffset.UtcNow,
            }),
        };
        var handler = new AddWebContentCommentCommandHandler(
            new FakeWebParticipantProfileResolver(userId: 700), content,
            NullLogger<AddWebContentCommentCommandHandler>.Instance);

        var m = await handler.Handle(
            new AddWebContentCommentCommand { ContentId = "c1", Body = "Merhaba" }, default);

        m!.Body.Should().Be("Merhaba");
        m.Status.Should().Be(ContentCommentStatus.Pending, "the caller sees their own comment's status");
    }
}
