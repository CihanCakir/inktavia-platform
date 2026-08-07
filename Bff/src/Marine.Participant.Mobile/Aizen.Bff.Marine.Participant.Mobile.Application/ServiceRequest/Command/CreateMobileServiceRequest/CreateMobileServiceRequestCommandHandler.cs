using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// Create orchestration (BE_MO1). The module has no cross-call transaction, so this composes:
///   1) POST /service-requests — REQUIRED. Creates the Draft with the asserted caller as OwnerUserId.
///   2) attach any already-uploaded files (best-effort).
///   3) PATCH /publish (best-effort) when the wizard submitted (Draft → Open).
/// Step 1 is authoritative. The module builds its create response from the entity BEFORE the unit-of-work commits,
/// so the returned Id is 0 — the row IS committed by the time this returns, so the real Id is recovered
/// deterministically by the just-created request's unique RequestCode from the owner's list (an owner has a small
/// set, and the list is uncached, so the fresh row is present). Steps 2–3 are best-effort — a failure is logged,
/// and the returned detail reflects exactly what persisted, so the client never sees a success that hides a
/// dropped attachment or an unpublished request.
/// </summary>
public sealed class CreateMobileServiceRequestCommandHandler
    : AizenCommandHandler<CreateMobileServiceRequestCommand, MobileServiceRequestDetailDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 50;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<CreateMobileServiceRequestCommandHandler> _logger;

    public CreateMobileServiceRequestCommandHandler(
        IParticipantProfileResolver resolver,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        ILogger<CreateMobileServiceRequestCommandHandler> logger)
    {
        _resolver = resolver;
        _sr = sr;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileServiceRequestDetailDto?> Handle(
        CreateMobileServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request ?? throw new AizenBusinessException("Service request payload is required.");

        if (r.VesselId <= 0)
            throw new AizenBusinessException("A vessel is required.");
        if (string.IsNullOrWhiteSpace(r.ServiceCategoryCode))
            throw new AizenBusinessException("A service category is required.");
        if (string.IsNullOrWhiteSpace(r.Title))
            throw new AizenBusinessException("A title is required.");

        // Resolve → sets the identity holder so the create asserts as this participant (OwnerUserId = caller).
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // 1) Create the Draft (required).
        var createResp = await _sr.Create(new CreateServiceRequestRequest
        {
            VesselId = r.VesselId,
            ServiceCategoryCode = r.ServiceCategoryCode.Trim(),
            ServiceTypeCode = NullIfBlank(r.ServiceTypeCode),
            Title = r.Title.Trim(),
            Description = NullIfBlank(r.Description),
            Priority = MobileServiceRequestMapper.ParsePriority(r.Priority),
            RequestedStartDate = r.RequestedStartDate,
            RequestedEndDate = r.RequestedEndDate,
            LocationCountryCode = NullIfBlank(r.LocationCountryCode),
            LocationCityCode = NullIfBlank(r.LocationCityCode),
            LocationMarinaName = NullIfBlank(r.LocationMarinaName),
            LocationLatitude = r.LocationLatitude,
            LocationLongitude = r.LocationLongitude,
            OwnerNotes = NullIfBlank(r.OwnerNotes),
        });

        var created = createResp?.Body?.ServiceRequest;
        if (created is null || string.IsNullOrWhiteSpace(created.RequestCode))
            throw new AizenBusinessException("Service request could not be created.");

        // Recover the committed id by the just-created request's unique RequestCode (create response Id is 0 pre-commit).
        var serviceRequestId = created.Id;
        if (serviceRequestId <= 0)
        {
            var mine = await _sr.GetMy(new Aizen.Modules.ServiceRequest.Abstraction.Request.Filter.ServiceRequestListFilterRequest
            {
                PageIndex = OwnedPageIndex,
                PageSize = OwnedPageSize,
            });
            serviceRequestId = mine?.Body?.Items?
                .FirstOrDefault(x => x.RequestCode == created.RequestCode)?.Id ?? 0;
        }
        if (serviceRequestId <= 0)
            throw new AizenBusinessException("Service request could not be created.");

        // 2) Attachments (optional, best-effort) — files already uploaded client-side via /mobile/uploads.
        foreach (var att in r.Attachments ?? Enumerable.Empty<MobileServiceRequestAttachmentInput>())
        {
            if (att.FileId == Guid.Empty) continue;
            try
            {
                await _sr.AddAttachment(serviceRequestId, new AddServiceRequestAttachmentRequest
                {
                    FileId = att.FileId,
                    AttachmentType = MobileServiceRequestMapper.ParseAttachmentType(att.AttachmentType),
                    Title = NullIfBlank(att.Title),
                    Description = NullIfBlank(att.Description),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Service request {ServiceRequestId} created but attachment {FileId} failed.", serviceRequestId, att.FileId);
            }
        }

        // 3) Publish (optional, best-effort) — submit the Draft to providers (Draft → Open).
        if (r.Publish)
        {
            try
            {
                await _sr.Publish(serviceRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Service request {ServiceRequestId} created but publish failed (stays Draft).", serviceRequestId);
            }
        }

        // Return the assembled detail so the client sees exactly what persisted.
        var detailResp = await _sr.GetDetail(serviceRequestId);
        var detail = detailResp?.Body?.Detail;
        if (detail is not null)
            return await MobileServiceRequestMapper.MapDetailWithAttachmentUrlsAsync(detail, _fileStorage, _logger, cancellationToken);

        // Detail read failed post-create — echo a minimal projection rather than 500.
        return new MobileServiceRequestDetailDto
        {
            Id = serviceRequestId,
            RequestCode = created.RequestCode,
            VesselId = created.VesselId,
            ServiceCategoryCode = created.ServiceCategoryCode,
            ServiceTypeCode = created.ServiceTypeCode,
            Title = created.Title,
            Description = created.Description,
            Status = created.Status.ToString(),
            Priority = created.Priority.ToString(),
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            CanEdit = true,
            CanCancel = true,
        };
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
