using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner's service-request list. Resolves the participant (sets the identity holder so the module scopes to
/// this owner via the assertion), reads the module `my` list, and maps to the mobile list contract. Returns an
/// empty list on any failure (mirrors the mobile Vessel list) so the screen renders an empty state, never a 500.
/// </summary>
public sealed class GetMobileMyServiceRequestsQueryHandler
    : AizenQueryHandler<GetMobileMyServiceRequestsQuery, MobileServiceRequestListDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly ILogger<GetMobileMyServiceRequestsQueryHandler> _logger;

    public GetMobileMyServiceRequestsQueryHandler(
        IParticipantProfileResolver resolver,
        IServiceRequestRemoteCall sr,
        ILogger<GetMobileMyServiceRequestsQueryHandler> logger)
    {
        _resolver = resolver;
        _sr = sr;
        _logger = logger;
    }

    public override async Task<MobileServiceRequestListDto?> Handle(
        GetMobileMyServiceRequestsQuery request, CancellationToken cancellationToken)
    {
        var empty = new MobileServiceRequestListDto { PageIndex = request.PageIndex, PageSize = request.PageSize };
        try
        {
            var resolution = await _resolver.ResolveAsync(cancellationToken);
            if (resolution.ProfileId is not > 0)
                return empty;

            var resp = await _sr.GetMy(new ServiceRequestListFilterRequest
            {
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            });

            var body = resp?.Body;
            var items = (body?.Items ?? new())
                .Select(MobileServiceRequestMapper.MapListItem)
                .ToList();

            return new MobileServiceRequestListDto
            {
                Items = items,
                TotalCount = body?.TotalCount ?? items.Count,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Participant service-request list failed.");
            return empty;
        }
    }
}
