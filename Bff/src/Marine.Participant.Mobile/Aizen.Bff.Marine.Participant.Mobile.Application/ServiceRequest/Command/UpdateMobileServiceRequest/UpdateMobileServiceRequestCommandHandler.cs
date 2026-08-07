using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// Update one of the caller's own service requests. Resolves the participant, gates the id against the resolved
/// owner (the module does not owner-check update), maps the mobile edit to the module's full update, and returns
/// the re-read detail so the client reflects the change immediately.
/// </summary>
public sealed class UpdateMobileServiceRequestCommandHandler
    : AizenCommandHandler<UpdateMobileServiceRequestCommand, MobileServiceRequestDetailDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<UpdateMobileServiceRequestCommandHandler> _logger;

    public UpdateMobileServiceRequestCommandHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        ILogger<UpdateMobileServiceRequestCommandHandler> logger)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileServiceRequestDetailDto?> Handle(
        UpdateMobileServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request ?? throw new AizenBusinessException("Service request payload is required.");
        if (string.IsNullOrWhiteSpace(r.Title))
            throw new AizenBusinessException("A title is required.");
        if (string.IsNullOrWhiteSpace(r.ServiceCategoryCode))
            throw new AizenBusinessException("A service category is required.");

        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        await _sr.Update(request.ServiceRequestId, new UpdateServiceRequestRequest
        {
            Title = r.Title.Trim(),
            Description = NullIfBlank(r.Description),
            ServiceCategoryCode = r.ServiceCategoryCode.Trim(),
            ServiceTypeCode = NullIfBlank(r.ServiceTypeCode),
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

        var detailResp = await _sr.GetDetail(request.ServiceRequestId);
        var detail = detailResp?.Body?.Detail
            ?? throw new AizenBusinessException("Service request not found.");

        return await MobileServiceRequestMapper.MapDetailWithAttachmentUrlsAsync(detail, _fileStorage, _logger, cancellationToken);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
