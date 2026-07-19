using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderConversations;

/// <summary>
/// Returns the calling provider's conversations: requests where they have an offer or messages.
/// One grouped query — no N+1.
/// </summary>
public sealed class GetProviderConversationsQueryHandler
    : AizenQueryHandler<GetProviderConversationsQuery, GetProviderConversationsResponse>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;

    public GetProviderConversationsQueryHandler(ServiceRequestDbContext db, IAizenInfoAccessor info)
    {
        _db = db;
        _info = info;
    }

    public override async Task<GetProviderConversationsResponse?> Handle(
        GetProviderConversationsQuery request, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var userId = _info.UserInfoAccessor.UserInfo.UserId;

        // Find all SR IDs this provider has a relationship with (offer or message)
        var srIdsFromOffers = _db.ServiceRequestOffers
            .Where(o => o.ProviderProfileId == profileId && !o.IsDeleted)
            .Select(o => o.ServiceRequestId);

        var srIdsFromMessages = _db.ServiceRequestMessages
            .Where(m => m.SenderUserId == userId && !m.IsDeleted)
            .Select(m => m.ServiceRequestId);

        var relatedSrIds = await srIdsFromOffers.Union(srIdsFromMessages).Distinct().ToListAsync(ct);

        if (relatedSrIds.Count == 0)
            return new GetProviderConversationsResponse();

        // Build conversation list in one query
        var conversations = await _db.ServiceRequests
            .AsNoTracking()
            .Where(sr => relatedSrIds.Contains(sr.Id) && !sr.IsDeleted)
            .Select(sr => new ProviderConversationDto
            {
                ServiceRequestId = sr.Id,
                RequestCode = sr.RequestCode,
                Title = sr.Title,
                LastMessagePreview = sr.Messages
                    .Where(m => !m.IsDeleted && m.MessageType == ServiceRequestMessageType.Text)
                    .OrderByDescending(m => m.CreateDate)
                    .Select(m => m.Content.Length > 80 ? m.Content.Substring(0, 80) : m.Content)
                    .FirstOrDefault(),
                LastMessageType = sr.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.CreateDate)
                    .Select(m => (int)m.MessageType)
                    .FirstOrDefault(),
                LastMessageAt = sr.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.CreateDate)
                    .Select(m => m.CreateDate)
                    .FirstOrDefault(),
                UnreadCount = sr.Messages
                    .Count(m => !m.IsDeleted && m.SenderUserId != userId && !m.IsRead),
                ChannelOpen = sr.Messages
                    .Any(m => !m.IsDeleted && m.SenderType == ServiceRequestMessageSenderType.Owner),
                LifecycleStatus = sr.Messages
                    .Where(m => !m.IsDeleted && m.MessageType == ServiceRequestMessageType.StatusChange)
                    .OrderByDescending(m => m.CreateDate)
                    .Select(m => m.Content)
                    .FirstOrDefault()
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

        return new GetProviderConversationsResponse { Conversations = conversations };
    }
}
