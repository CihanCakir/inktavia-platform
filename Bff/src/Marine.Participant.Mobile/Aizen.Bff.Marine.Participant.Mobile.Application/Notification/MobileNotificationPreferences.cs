using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Request;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

/// <summary>GET /api/v1/mobile/notifications/preferences — the caller's per-category × channel preference matrix.</summary>
public sealed class GetMobileNotificationPreferencesQuery : AizenQuery<MobileNotificationPreferencesDto> { }

public sealed class GetMobileNotificationPreferencesQueryHandler
    : AizenQueryHandler<GetMobileNotificationPreferencesQuery, MobileNotificationPreferencesDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly INotificationRemoteCall _notification;

    public GetMobileNotificationPreferencesQueryHandler(IParticipantProfileResolver resolver, INotificationRemoteCall notification)
    {
        _resolver = resolver;
        _notification = notification;
    }

    public override async Task<MobileNotificationPreferencesDto?> Handle(
        GetMobileNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _notification.GetPreferences();
        return MobileNotificationMapper.MapPreferences(resp?.Body);
    }
}

/// <summary>PUT /api/v1/mobile/notifications/preferences — toggle one category×channel cell (only Push/Email are
/// user-changeable; the module rejects locked cells). Returns the refreshed matrix.</summary>
public sealed class UpdateMobileNotificationPreferenceCommand : AizenCommand<MobileNotificationPreferencesDto>
{
    public UpdateMobileNotificationPreferenceCommand(MobileUpdatePreferenceRequest request) => Request = request;
    public MobileUpdatePreferenceRequest Request { get; }
}

public sealed class UpdateMobileNotificationPreferenceCommandHandler
    : AizenCommandHandler<UpdateMobileNotificationPreferenceCommand, MobileNotificationPreferencesDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly INotificationRemoteCall _notification;

    public UpdateMobileNotificationPreferenceCommandHandler(IParticipantProfileResolver resolver, INotificationRemoteCall notification)
    {
        _resolver = resolver;
        _notification = notification;
    }

    public override async Task<MobileNotificationPreferencesDto?> Handle(
        UpdateMobileNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var r = request.Request;
        if (r is null || string.IsNullOrWhiteSpace(r.Category) || string.IsNullOrWhiteSpace(r.Channel))
            throw new AizenBusinessException("A category and channel are required.");

        var resp = await _notification.UpdatePreference(new UpdateNotificationPreferenceRequest
        {
            Category = r.Category!.Trim(),
            Channel  = r.Channel!.Trim(),
            Enabled  = r.Enabled,
        });

        return MobileNotificationMapper.MapPreferences(resp?.Body);
    }
}
