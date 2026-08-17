using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// Cancel one of the caller's own service requests with the N-E structured reason (auto-mapped module-side to a
/// refund reason) + optional note. Resolves the participant, gates the id against the resolved owner, cancels via
/// the module, and returns the re-read detail so the client shows Cancelled + the new timeline entry.
/// </summary>
public sealed class CancelMobileServiceRequestCommandHandler
    : AizenCommandHandler<CancelMobileServiceRequestCommand, MobileServiceRequestDetailDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<CancelMobileServiceRequestCommandHandler> _logger;

    public CancelMobileServiceRequestCommandHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        ILogger<CancelMobileServiceRequestCommandHandler> logger)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileServiceRequestDetailDto?> Handle(
        CancelMobileServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        await _sr.Cancel(request.ServiceRequestId, new CancelServiceRequestRequest
        {
            ReasonCode = MobileServiceRequestMapper.ParseCancelReason(request.Request?.ReasonCode),
            Reason = string.IsNullOrWhiteSpace(request.Request?.Note) ? null : request.Request!.Note!.Trim(),
        });

        var detailResp = await _sr.GetDetail(request.ServiceRequestId);
        var detail = detailResp?.Body?.Detail
            ?? throw new AizenBusinessException("Service request not found.");

        return await MobileServiceRequestMapper.MapDetailWithAttachmentUrlsAsync(detail, _fileStorage, _logger, cancellationToken);
    }
}
