using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Reference;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Reference;

public sealed class GetReferenceItemsQueryHandler
    : AizenQueryHandler<GetReferenceItemsQuery, List<ReferenceItemDto>>
{
    private readonly IReferenceDataRemoteCall _reference;
    private readonly ILogger<GetReferenceItemsQueryHandler> _logger;

    public GetReferenceItemsQueryHandler(
        IReferenceDataRemoteCall reference, ILogger<GetReferenceItemsQueryHandler> logger)
    {
        _reference = reference;
        _logger = logger;
    }

    public override async Task<List<ReferenceItemDto>?> Handle(
        GetReferenceItemsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _reference.GetLookupItems(request.GroupCode);
            var items = result?.Body ?? new List<Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem.LookupItemDto>();
            return items
                .OrderBy(i => i.SortOrder)
                .Select(i => new ReferenceItemDto
                {
                    Code = i.Code,
                    Name = i.Name,
                    Description = i.Description,
                    IconKey = i.IconKey,
                    SortOrder = i.SortOrder,
                    IsDefault = i.IsDefault,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reference lookup failed for group {GroupCode}.", request.GroupCode);
            return new List<ReferenceItemDto>();
        }
    }
}
