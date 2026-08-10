using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Query.Owner.GetOwnerAttachmentAccessCheck;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-MO10b — the owner-scoped attachment access-check core (<c>GetOwnerAttachmentAccessCheckQueryHandler.Evaluate</c>).
/// The owner may mint a read-url for a fileId only when they OWN the SR and the fileId is actually on it (here: a chat
/// MESSAGE attachment — where an owner-sent image lands via the SR send). Every failure is a vague not-found (no info
/// leak). This is the security-load-bearing logic; the handler is a thin loader around it.
/// </summary>
public sealed class OwnerAttachmentAccessMo10bTests
{
    private const long OwnerId = 100011;
    private static readonly Guid FileOnSr = Guid.NewGuid();

    private static ServiceRequestEntity SrWithMessageImage(long ownerUserId)
    {
        var sr = ServiceRequestEntity.Create(
            requestCode: "SR-CHAT-1", ownerUserId: ownerUserId, vesselId: 3,
            serviceCategoryCode: "electrical", serviceTypeCode: null,
            title: "Fix wiring", description: null, priority: ServiceRequestPriority.Normal,
            requestedStartDate: null, requestedEndDate: null,
            locationCountryCode: "TR", locationCityCode: "35", locationMarinaName: null,
            locationLatitude: null, locationLongitude: null, ownerNotes: null, expiresAt: null);

        // An owner-sent image message — its AttachmentFileId lands in sr.Messages (the task #81 consistency).
        sr.AddMessage(ServiceRequestMessageEntity.Create(
            sr.Id, senderUserId: ownerUserId, ServiceRequestMessageSenderType.Owner,
            ServiceRequestMessageType.Image, content: string.Empty, attachmentFileId: FileOnSr));
        return sr;
    }

    // (3a) owner owns the SR + the fileId is a message attachment on it → authorized.
    [Fact]
    public void Owner_gets_access_for_a_file_on_their_own_sr()
    {
        var sr = SrWithMessageImage(OwnerId);

        var result = GetOwnerAttachmentAccessCheckQueryHandler.Evaluate(sr, OwnerId, FileOnSr);

        result!.Authorized.Should().BeTrue();
        result.FileId.Should().Be(FileOnSr);
    }

    // (3b) a fileId NOT on the SR → vague not-found.
    [Fact]
    public void A_file_not_on_the_sr_is_rejected()
    {
        var sr = SrWithMessageImage(OwnerId);

        var act = () => GetOwnerAttachmentAccessCheckQueryHandler.Evaluate(sr, OwnerId, Guid.NewGuid());

        act.Should().Throw<AizenBusinessException>().WithMessage("*not found*");
    }

    // (3b) an SR the caller does NOT own → vague not-found (never leaks another owner's file).
    [Fact]
    public void A_foreign_sr_is_rejected()
    {
        var sr = SrWithMessageImage(ownerUserId: 999);   // owned by someone else

        var act = () => GetOwnerAttachmentAccessCheckQueryHandler.Evaluate(sr, OwnerId, FileOnSr);

        act.Should().Throw<AizenBusinessException>().WithMessage("*not found*");
    }

    // unresolved owner / missing SR → vague not-found.
    [Fact]
    public void Unresolved_owner_or_missing_sr_is_rejected()
    {
        var unresolvedOwner = () => GetOwnerAttachmentAccessCheckQueryHandler.Evaluate(SrWithMessageImage(OwnerId), ownerUserId: 0, FileOnSr);
        unresolvedOwner.Should().Throw<AizenBusinessException>();

        var missingSr = () => GetOwnerAttachmentAccessCheckQueryHandler.Evaluate(sr: null, OwnerId, FileOnSr);
        missingSr.Should().Throw<AizenBusinessException>();
    }
}
