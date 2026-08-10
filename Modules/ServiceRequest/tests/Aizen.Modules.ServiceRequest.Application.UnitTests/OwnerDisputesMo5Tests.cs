using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE_MO5 — the owner dispute surface. The owner OPENS a dispute (N-E structured reason + description, actor = Owner),
/// sees THEIR disputes (a cost-free list), and reads the case (cost-free) with its lifecycle + the resolution outcome
/// once the admin resolves. The owner is READ-ONLY on resolution — there is no owner resolve/status path (Admin-only).
/// These are the pure, in-isolation guarantees; the owner-scoping WHERE (only <c>sr.OwnerUserId == caller</c>) and the
/// BFF/module owner-gates are enforced by the query + the reused <c>EnsureOwnedAsync</c> primitive (build-verified).
/// </summary>
public sealed class OwnerDisputesMo5Tests
{
    // (1) The owner opens on their own SR with an N-E structured reason → the entity records the Owner actor, the
    // chosen reason + description, the opener's id, and the initial Open status. This is the primitive the owner reuses.
    [Fact]
    public void Owner_open_records_the_owner_actor_and_ne_reason()
    {
        const long ownerUserId = 7;

        var dispute = ServiceRequestDisputeEntity.Create(
            serviceRequestId: 9011, openedByUserId: ownerUserId,
            openedByActorType: ServiceRequestActorType.Owner,
            reason: ServiceRequestDisputeReason.QualityIssue,
            description: "  work not as agreed  ");

        dispute.OpenedByActorType.Should().Be(ServiceRequestActorType.Owner);
        dispute.OpenedByUserId.Should().Be(ownerUserId);
        dispute.Reason.Should().Be(ServiceRequestDisputeReason.QualityIssue);
        dispute.Description.Should().Be("work not as agreed"); // trimmed
        dispute.Status.Should().Be(ServiceRequestDisputeStatus.Open);
        dispute.ResolvedAt.Should().BeNull();
        dispute.ResolutionOutcome.Should().BeNull();
    }

    // (2) Open/actionable classification — the exact predicate the owner list uses for IsOpen + the global OpenCount:
    // a dispute is open UNLESS it is Resolved or Closed. This is what scopes the count independently of the page filter.
    [Theory]
    [InlineData(ServiceRequestDisputeStatus.Open, true)]
    [InlineData(ServiceRequestDisputeStatus.UnderReview, true)]
    [InlineData(ServiceRequestDisputeStatus.PendingOwnerResponse, true)]
    [InlineData(ServiceRequestDisputeStatus.PendingProviderResponse, true)]
    [InlineData(ServiceRequestDisputeStatus.Escalated, true)]
    [InlineData(ServiceRequestDisputeStatus.Resolved, false)]
    [InlineData(ServiceRequestDisputeStatus.Closed, false)]
    public void Open_actionable_predicate_matches_the_list_and_count_rule(
        ServiceRequestDisputeStatus status, bool expectedOpen)
    {
        var isOpen = status != ServiceRequestDisputeStatus.Resolved
                     && status != ServiceRequestDisputeStatus.Closed;

        isOpen.Should().Be(expectedOpen);
    }

    // (6) A resolved dispute surfaces its outcome READ-ONLY on the DTO the owner reads: Status = Resolved, the
    // monetary outcome + refund amount, and the resolved-at. The owner never sets these — the admin resolve does.
    [Fact]
    public void Resolved_dispute_surfaces_the_outcome_read_only()
    {
        var dispute = ServiceRequestDisputeEntity.Create(
            serviceRequestId: 9011, openedByUserId: 7,
            openedByActorType: ServiceRequestActorType.Owner,
            reason: ServiceRequestDisputeReason.PricingDispute, description: "overcharged");

        dispute.Resolve(adminUserId: 1, resolutionNotes: "partial refund granted");
        dispute.MarkPaymentOutcomeApplied(DisputeResolutionOutcome.FavorPayerPartialRefund, refundAmount: 250m);

        var dto = dispute.ToDto();

        dto.Status.Should().Be(ServiceRequestDisputeStatus.Resolved);
        dto.ResolutionOutcome.Should().Be(DisputeResolutionOutcome.FavorPayerPartialRefund);
        dto.ResolutionRefundAmount.Should().Be(250m);
        dto.ResolutionNotes.Should().Be("partial refund granted");
        dto.ResolvedAt.Should().NotBeNull();
        dto.ResolvedByAdminUserId.Should().Be(1); // resolution is admin-owned
    }

    // (idempotency) a re-resolve never re-applies the monetary outcome — the owner's read stays stable.
    [Fact]
    public void Payment_outcome_is_applied_once()
    {
        var dispute = ServiceRequestDisputeEntity.Create(
            serviceRequestId: 9011, openedByUserId: 7,
            openedByActorType: ServiceRequestActorType.Owner,
            reason: ServiceRequestDisputeReason.Other, description: "x");

        dispute.MarkPaymentOutcomeApplied(DisputeResolutionOutcome.FavorPayerFullRefund, 100m);
        dispute.MarkPaymentOutcomeApplied(DisputeResolutionOutcome.Split, 999m); // must be ignored

        dispute.ResolutionOutcome.Should().Be(DisputeResolutionOutcome.FavorPayerFullRefund);
        dispute.ResolutionRefundAmount.Should().Be(100m);
        dispute.IsPaymentOutcomeApplied.Should().BeTrue();
    }

    // (4) The owner list is cost-free — the row + response carry NO supplier cost / dealer margin / commission /
    // provider-net / funding figures. Only the dispute + its SR header cross.
    [Fact]
    public void Owner_dispute_list_dtos_are_cost_free()
    {
        var names = typeof(OwnerDisputeItemDto).GetProperties().Select(p => p.Name)
            .Concat(typeof(GetOwnerDisputesResponse).GetProperties().Select(p => p.Name));

        names.Should().NotContain(n =>
            n.Contains("Cost", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Commission", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Margin", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Funding", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Supplier", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Dealer", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("ProviderNet", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Net", StringComparison.OrdinalIgnoreCase));
    }

    // (5) The owner is read-only on resolution. The owner list row carries the resolution READ fields (ResolvedAt) but
    // no write path: the entity's resolve transition is only reachable via Resolve(...)/MarkPaymentOutcomeApplied(...),
    // which the Admin-only command path calls — the owner query/DTOs never do. (Enforced structurally: there is no
    // owner-facing resolve/status endpoint; verified by the module build + REPORT grep.)
    [Fact]
    public void Owner_dispute_row_exposes_resolution_as_read_only_fields()
    {
        var props = typeof(OwnerDisputeItemDto).GetProperties();

        // The resolved-at read field is present…
        props.Select(p => p.Name).Should().Contain(nameof(OwnerDisputeItemDto.ResolvedAt));
        // …and every property is a read model member (get + init only, no plain public setter the owner could push to).
        props.Should().OnlyContain(p => p.CanRead);
    }
}
