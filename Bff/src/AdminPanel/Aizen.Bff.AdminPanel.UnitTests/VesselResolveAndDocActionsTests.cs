using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Vessels.Command;
using Aizen.Bff.AdminPanel.Application.Vessels.Query;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aizen.Bff.AdminPanel.UnitTests;

/// <summary>
/// BE_VESSEL_RESOLVE_AND_DOC_ACTIONS — the vessel-detail id→name/URL resolution + document approve happen at the BFF
/// (the Vessel module stays boundary-pure). These pin the composition: the detail handler resolves owner display
/// names (Identity) + presigns media/document files (FileStorage) inside one pass, degrades to empty + a warning on a
/// downstream failure (never throws), and the approve command is a thin passthrough to the module.
/// </summary>
public sealed class VesselResolveAndDocActionsTests
{
    private static AizenApiResponse<T> Ok<T>(T body) where T : class => new(AizenResponseHeader.Success(), body);

    private static UserProfileListItemDto Profile(long userId, string first, string last, string? photo = null, string? email = null) =>
        new() { UserId = userId, FirstName = first, LastName = last, ProfilePhotoUrl = photo, Email = email };

    private static VesselDetailDto Detail(Guid mediaFileId, Guid docFileId, long ownerUserId, long approverUserId) => new()
    {
        Vessel = new VesselDto { Id = 100013, Name = "Aegean Wind" },
        Owners = new List<VesselOwnerDto>
        {
            new() { Id = 1, VesselId = 100013, UserId = ownerUserId, Role = VesselOwnershipRole.PrimaryOwner, Status = VesselOwnershipStatus.Active, IsPrimary = true },
        },
        Media = new List<VesselMediaDto>
        {
            new() { Id = 1, VesselId = 100013, MediaType = VesselMediaType.Photo, FileId = mediaFileId, IsCover = true },
        },
        Documents = new List<VesselDocumentDto>
        {
            new() { Id = 1, VesselId = 100013, DocumentTypeCode = "REGISTRATION", DocumentName = "Reg", FileId = docFileId, Status = VesselDocumentStatus.Active, ApprovedByUserId = approverUserId, ApprovedAt = DateTime.UtcNow },
        },
    };

    private static (GetVesselByIdBffQueryHandler handler, IVesselRemoteCall vessel, IIdentityRemoteCall identity, IFileStorageRemoteCall files) Build()
    {
        var vessel = Substitute.For<IVesselRemoteCall>();
        var identity = Substitute.For<IIdentityRemoteCall>();
        var files = Substitute.For<IFileStorageRemoteCall>();
        return (new GetVesselByIdBffQueryHandler(vessel, identity, files), vessel, identity, files);
    }

    // ── A + B + C(read): owner name resolved, media/doc presigned, approvedByName resolved ───────
    [Fact]
    public async Task Detail_resolves_owner_name_presigns_files_and_resolves_approver_name()
    {
        var mediaFileId = Guid.NewGuid();
        var docFileId = Guid.NewGuid();
        const long ownerUserId = 100029;

        var (handler, vessel, identity, files) = Build();
        vessel.GetVesselById(100013).Returns(Ok(new GetVesselDetailResponse(Detail(mediaFileId, docFileId, ownerUserId, ownerUserId))));
        identity.GetUserProfilesByUserIds(Arg.Any<long[]>())
                .Returns(Ok(new List<UserProfileListItemDto> { Profile(ownerUserId, "Ada", "Yılmaz", photo: "https://cdn/ada.png", email: "ada@x.com") }));
        files.CreateReadUrl(Arg.Any<Guid>(), Arg.Any<CreateFileReadUrlRemoteCallRequest>())
             .Returns(Ok(new FileAccessUrlDto { ReadUrl = "https://minio/presigned" }));

        var result = await handler.Handle(new GetVesselByIdBffQuery(100013), CancellationToken.None);

        var owner = result!.Vessel!.Owners.Single();
        owner.Name.Should().Be("Ada Yılmaz");
        owner.AvatarUrl.Should().Be("https://cdn/ada.png");
        owner.Email.Should().Be("ada@x.com");
        result.Vessel.Media.Single().Url.Should().Be("https://minio/presigned");
        result.Vessel.Documents.Single().FileUrl.Should().Be("https://minio/presigned");
        result.Vessel.Documents.Single().ApprovedByName.Should().Be("Ada Yılmaz");
        result.Warnings.Should().BeEmpty();

        // One Identity batch carrying the deduped user ids (owner == approver here → a single id).
        await identity.Received(1).GetUserProfilesByUserIds(Arg.Is<long[]>(ids => ids.Length == 1 && ids[0] == ownerUserId));
    }

    // ── A best-effort: Identity down → owner name empty + a warning, never throws ────────────────
    [Fact]
    public async Task Detail_identity_failure_leaves_names_empty_and_appends_warning()
    {
        var (handler, vessel, identity, files) = Build();
        vessel.GetVesselById(100013).Returns(Ok(new GetVesselDetailResponse(Detail(Guid.NewGuid(), Guid.NewGuid(), 100029, 100029))));
        identity.GetUserProfilesByUserIds(Arg.Any<long[]>()).Throws(new Exception("Identity down"));
        files.CreateReadUrl(Arg.Any<Guid>(), Arg.Any<CreateFileReadUrlRemoteCallRequest>())
             .Returns(Ok(new FileAccessUrlDto { ReadUrl = "https://minio/presigned" }));

        var result = await handler.Handle(new GetVesselByIdBffQuery(100013), CancellationToken.None);

        result!.Vessel!.Owners.Single().Name.Should().BeNull();          // degraded, FE falls back to #userId
        result.Vessel.Documents.Single().ApprovedByName.Should().BeNull();
        result.Vessel.Media.Single().Url.Should().Be("https://minio/presigned"); // presign still succeeded
        result.Warnings.Should().Contain(w => w.Module == "Identity");
    }

    // ── B best-effort: a presign failure drops that item's URL + a warning, HTTP 200 (no throw) ──
    [Fact]
    public async Task Detail_presign_failure_drops_url_and_appends_warning()
    {
        var (handler, vessel, identity, files) = Build();
        vessel.GetVesselById(100013).Returns(Ok(new GetVesselDetailResponse(Detail(Guid.NewGuid(), Guid.NewGuid(), 100029, 100029))));
        identity.GetUserProfilesByUserIds(Arg.Any<long[]>())
                .Returns(Ok(new List<UserProfileListItemDto> { Profile(100029, "Ada", "Yılmaz") }));
        files.CreateReadUrl(Arg.Any<Guid>(), Arg.Any<CreateFileReadUrlRemoteCallRequest>())
             .Returns(Ok<FileAccessUrlDto>(null!)); // objectless / unpresignable file

        var result = await handler.Handle(new GetVesselByIdBffQuery(100013), CancellationToken.None);

        result!.Vessel!.Media.Single().Url.Should().BeNull();
        result.Vessel.Documents.Single().FileUrl.Should().BeNull();
        result.Vessel.Owners.Single().Name.Should().Be("Ada Yılmaz"); // owner resolve still succeeded
        result.Warnings.Should().Contain(w => w.Module == "FileStorage");
    }

    // ── C: the approve BFF command is a thin passthrough to the module ──────────────────────────
    [Fact]
    public async Task Approve_command_delegates_to_the_module_and_returns_the_document()
    {
        var vessel = Substitute.For<IVesselRemoteCall>();
        var approvedAt = DateTime.UtcNow;
        vessel.ApproveVesselDocument(100013, 100001)
              .Returns(Ok(new ApproveVesselDocumentResponse(new VesselDocumentDto { Id = 100001, ApprovedAt = approvedAt, ApprovedByUserId = 5 })));

        var handler = new ApproveVesselDocumentBffCommandHandler(vessel);
        var result = await handler.Handle(new ApproveVesselDocumentBffCommand(100013, 100001), CancellationToken.None);

        result!.Document.Id.Should().Be(100001);
        result.Document.ApprovedAt.Should().Be(approvedAt);
        await vessel.Received(1).ApproveVesselDocument(100013, 100001);
    }
}
