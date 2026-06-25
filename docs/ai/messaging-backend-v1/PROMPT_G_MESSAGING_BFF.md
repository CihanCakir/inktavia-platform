# PROMPT G — Messaging Module: BFF Layer
# `Aizen.Bff.AdminPanel` — Messaging Integration

## Context & Architecture Rules

**Confirmed BFF patterns from existing codebase — follow exactly:**
- Remote call interface: `IXxxRemoteCall : IAizenRemoteCall` with `[AizenRemoteCallGet/Post/Patch]`
- Headers: `[AizenRemoteCallHeader("Authorization")]`, `[AizenRemoteCallHeader("X-Aizen-User-Token")]`
- Query params: `[Refit.Query]` | Body: `[AizenRemoteCallBody]`
- Returns: `Task<AizenApiResponse<TResponse>>`
- BFF Controller: `AizenWebApiController`, `[Authorize(Policy = "AdminPanelAccess")]`
- Route prefix: `api/v1/admin-panel/`
- CQRS: BFF query/command handler → remote call → map to BFF DTO
- User token forwarded: `HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault()`

**Important:** The existing `IServiceRequestAdminBffRemoteCall` has stale Conversation endpoints pointing to ServiceRequest module. These must be **removed** and replaced by the new `IMessagingAdminBffRemoteCall` pointing to `Aizen.Modules.Messaging`.

---

## Step 1 — Remote Call Interface

File: `Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IMessagingAdminBffRemoteCall.cs`

```csharp
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Messaging.Abstraction.Request;
using Aizen.Modules.Messaging.Abstraction.Response.Conversation;
using Aizen.Modules.Messaging.Abstraction.Response.Reporting;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Messaging admin BFF remote call",
    "Defines synchronous BFF-to-Messaging calls for admin conversation oversight, moderation, and reporting.")]
public interface IMessagingAdminBffRemoteCall : IAizenRemoteCall
{
    // ── Conversations ────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/conversations")]
    Task<AizenApiResponse<GetConversationListResponse>> GetConversationList(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] string? contextType = null,
        [Refit.Query] string? status = null,
        [Refit.Query] int skip = 0,
        [Refit.Query] int take = 20);

    [AizenRemoteCallGet("/api/v1/conversations/{conversationId}")]
    Task<AizenApiResponse<GetConversationDetailResponse>> GetConversationDetail(
        long conversationId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/conversations/by-context")]
    Task<AizenApiResponse<GetConversationDetailResponse>> GetConversationByContext(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] string contextType = default!,
        [Refit.Query] long contextId = default);

    [AizenRemoteCallPost("/api/v1/conversations")]
    Task<AizenApiResponse<CreateConversationResponse>> CreateConversation(
        [AizenRemoteCallBody] CreateConversationRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    // ── Messages ─────────────────────────────────────────────────────

    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages")]
    Task<AizenApiResponse<SendMessageResponse>> SendMessage(
        long conversationId,
        [AizenRemoteCallBody] SendMessageRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPatch("/api/v1/conversations/{conversationId}/messages/mark-read")]
    Task<AizenApiResponse<bool>> MarkConversationRead(
        long conversationId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/conversations/{conversationId}/messages/attachment-upload-url")]
    Task<AizenApiResponse<RequestAttachmentUploadUrlResponse>> GetAttachmentUploadUrl(
        long conversationId,
        [AizenRemoteCallBody] AttachmentUploadUrlRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    // ── Moderation ───────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/moderation/queue")]
    Task<AizenApiResponse<GetModerationQueueResponse>> GetModerationQueue(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] int skip = 0,
        [Refit.Query] int take = 20);

    [AizenRemoteCallPatch("/api/v1/moderation/messages/{messageId}")]
    Task<AizenApiResponse<bool>> ModerateMessage(
        long messageId,
        [AizenRemoteCallBody] ModerateMessageRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPatch("/api/v1/moderation/conversations/{conversationId}/flag")]
    Task<AizenApiResponse<bool>> FlagConversation(
        long conversationId,
        [AizenRemoteCallBody] FlagConversationRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    // ── Reporting ────────────────────────────────────────────────────

    [AizenRemoteCallGet("/api/v1/reporting/messaging/provider-response-time")]
    Task<AizenApiResponse<GetProviderResponseTimeReportResponse>> GetProviderResponseTime(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] DateTimeOffset? from = null,
        [Refit.Query] DateTimeOffset? to = null,
        [Refit.Query] int take = 20);

    [AizenRemoteCallGet("/api/v1/reporting/messaging/channel-usage")]
    Task<AizenApiResponse<GetChannelUsageReportResponse>> GetChannelUsage(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] DateTimeOffset? from = null,
        [Refit.Query] DateTimeOffset? to = null);
}
```

---

## Step 2 — BFF DTOs

Folder: `Aizen.Bff.AdminPanel.Application/AdminMessaging/Dto/`

### `AdminConversationListBffResponse.cs`
```csharp
namespace Aizen.Bff.AdminPanel.Application.AdminMessaging.Dto;

[DocumentationInfo("Admin conversation list BFF response", "Paginated conversation list enriched with hub URL for admin panel.")]
public sealed class AdminConversationListBffResponse
{
    public List<AdminConversationListItemDto> Items    { get; set; } = [];
    public int                               Total     { get; set; }
    public string                            HubUrl    { get; set; } = string.Empty;
}

public sealed class AdminConversationListItemDto
{
    public long          Id                  { get; set; }
    public string        Title               { get; set; } = string.Empty;
    public string        ContextType         { get; set; } = string.Empty;
    public long          ContextId           { get; set; }
    public string        Status              { get; set; } = string.Empty;
    public int           UnreadCountByAdmin  { get; set; }
    public string        LastMessagePreview  { get; set; } = string.Empty;
    public DateTimeOffset LastMessageAt      { get; set; }
    public List<AdminConversationParticipantDto> Participants { get; set; } = [];
}

public sealed class AdminConversationParticipantDto
{
    public long   UserId      { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role        { get; set; } = string.Empty;
}
```

### `AdminConversationDetailBffResponse.cs`
```csharp
namespace Aizen.Bff.AdminPanel.Application.AdminMessaging.Dto;

[DocumentationInfo("Admin conversation detail BFF response", "Full conversation thread with messages, participants, and hub URL.")]
public sealed class AdminConversationDetailBffResponse
{
    public long          Id                  { get; set; }
    public string        Title               { get; set; } = string.Empty;
    public string        ContextType         { get; set; } = string.Empty;
    public long          ContextId           { get; set; }
    public string        Status              { get; set; } = string.Empty;
    public string        HubUrl              { get; set; } = string.Empty;  // "/hubs/messaging"
    public List<AdminConversationParticipantDto> Participants { get; set; } = [];
    public List<AdminMessageDto>                 Messages    { get; set; } = [];
}

public sealed class AdminMessageDto
{
    public long          Id                { get; set; }
    public long          SenderUserId      { get; set; }
    public string        SenderName        { get; set; } = string.Empty;
    public string        SenderRole        { get; set; } = string.Empty;
    public string        Content           { get; set; } = string.Empty;
    public string        Type              { get; set; } = string.Empty;
    public bool          IsInternalNote    { get; set; }
    public string        ModerationStatus  { get; set; } = string.Empty;
    public string?       ModerationReason  { get; set; }
    public DateTimeOffset SentAt           { get; set; }
    public List<AdminAttachmentDto> Attachments { get; set; } = [];

    /// <summary>Parsed when Type == "Location". Null otherwise.</summary>
    public AdminLocationDto? Location      { get; set; }
}

public sealed class AdminAttachmentDto
{
    public long    Id            { get; set; }
    public string  FileName      { get; set; } = string.Empty;
    public string  FileType      { get; set; } = string.Empty;
    public string? ReadUrl       { get; set; }  // Presigned S3 URL (30min expiry)
}

public sealed class AdminLocationDto
{
    public double  Lat           { get; set; }
    public double  Lng           { get; set; }
    public string  Label         { get; set; } = string.Empty;
    public double? Accuracy      { get; set; }
    public string  GoogleMapsUrl => $"https://www.google.com/maps?q={Lat},{Lng}";
    public string  YandexMapsUrl => $"https://yandex.com/maps/?pt={Lng},{Lat}&z=16&l=map";
}
```

### `AdminModerationQueueBffResponse.cs`
```csharp
namespace Aizen.Bff.AdminPanel.Application.AdminMessaging.Dto;

[DocumentationInfo("Admin moderation queue BFF response", "Flagged/pending messages awaiting admin review.")]
public sealed class AdminModerationQueueBffResponse
{
    public List<AdminModerationItemDto> Items { get; set; } = [];
    public int Total { get; set; }
}

public sealed class AdminModerationItemDto
{
    public long          MessageId         { get; set; }
    public long          ConversationId    { get; set; }
    public string        ConversationTitle { get; set; } = string.Empty;
    public string        SenderName        { get; set; } = string.Empty;
    public string        SenderRole        { get; set; } = string.Empty;
    public string        ContentPreview    { get; set; } = string.Empty;
    public string        ModerationStatus  { get; set; } = string.Empty;
    public string?       ModerationReason  { get; set; }
    public DateTimeOffset SentAt           { get; set; }
    public TimeSpan      PendingFor        => DateTimeOffset.UtcNow - SentAt;
}
```

### `AdminMessagingReportBffResponse.cs`
```csharp
namespace Aizen.Bff.AdminPanel.Application.AdminMessaging.Dto;

[DocumentationInfo("Admin messaging report BFF response", "Combined provider response time and channel usage reports.")]
public sealed class AdminMessagingReportBffResponse
{
    public List<ProviderResponseTimeBffDto> ProviderResponseTime { get; set; } = [];
    public List<ChannelVolumeBffDto>        ChannelUsage         { get; set; } = [];
    public List<DailyTrendBffDto>           DailyTrend           { get; set; } = [];
    public List<PeakHourBffDto>             PeakHours            { get; set; } = [];
    public DateTimeOffset                   GeneratedAt          { get; set; }
}

public sealed class ProviderResponseTimeBffDto
{
    public long   ProviderUserId             { get; set; }
    public string ProviderName               { get; set; } = string.Empty;
    public double AvgFirstResponseMinutes    { get; set; }
    public int    TotalConversations         { get; set; }
    public int    UnansweredConversations    { get; set; }
}

public sealed class ChannelVolumeBffDto
{
    public string ContextType                    { get; set; } = string.Empty;
    public int    ConversationCount              { get; set; }
    public int    MessageCount                   { get; set; }
    public double AvgMessagesPerConversation     { get; set; }
}

public sealed class DailyTrendBffDto
{
    public string Date              { get; set; } = string.Empty;   // "2026-06-25"
    public int    MessageCount      { get; set; }
    public int    NewConversations  { get; set; }
}

public sealed class PeakHourBffDto
{
    public int Hour         { get; set; }
    public int MessageCount { get; set; }
}
```

---

## Step 3 — BFF Query Handlers

Folder: `Aizen.Bff.AdminPanel.Application/AdminMessaging/Query/`

### `GetAdminConversationListQuery.cs`
```csharp
using Aizen.Bff.AdminPanel.Application.AdminMessaging.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Abstraction;
using Microsoft.Extensions.Configuration;

namespace Aizen.Bff.AdminPanel.Application.AdminMessaging.Query;

public sealed record GetAdminConversationListQuery(
    string UserToken,
    string? ContextType,
    string? Status,
    int Skip,
    int Take
) : AizenQuery<AdminConversationListBffResponse>;

public sealed class GetAdminConversationListQueryHandler
    : AizenQueryHandler<GetAdminConversationListQuery, AdminConversationListBffResponse>
{
    private readonly IMessagingAdminBffRemoteCall _remote;
    private readonly IConfiguration _config;

    public GetAdminConversationListQueryHandler(
        IMessagingAdminBffRemoteCall remote, IConfiguration config)
    {
        _remote = remote;
        _config = config;
    }

    public override async Task<AdminConversationListBffResponse> Handle(
        GetAdminConversationListQuery query, CancellationToken ct)
    {
        var response = await _remote.GetConversationList(
            "Bearer internal", query.UserToken,
            query.ContextType, query.Status, query.Skip, query.Take);

        var hubUrl = _config["Messaging:HubUrl"] ?? "/hubs/messaging";
        var items  = response.Body?.Items ?? [];

        return new AdminConversationListBffResponse
        {
            Items  = items.Select(MapItem).ToList(),
            Total  = response.Body?.Total ?? 0,
            HubUrl = hubUrl
        };
    }

    private static AdminConversationListItemDto MapItem(ConversationListItemDto src) =>
        new()
        {
            Id                 = src.Id,
            Title              = src.Title,
            ContextType        = src.ContextType.ToString(),
            ContextId          = src.ContextId,
            Status             = src.Status.ToString(),
            UnreadCountByAdmin = src.UnreadCountByAdmin,
            LastMessagePreview = src.LastMessagePreview,
            LastMessageAt      = src.LastMessageAt,
            Participants       = src.Participants.Select(p => new AdminConversationParticipantDto
            {
                UserId      = p.UserId,
                DisplayName = p.DisplayName,
                Role        = p.Role.ToString()
            }).ToList()
        };
}
```

### `GetAdminConversationDetailQuery.cs`
```csharp
public sealed record GetAdminConversationDetailQuery(
    long ConversationId,
    string UserToken
) : AizenQuery<AdminConversationDetailBffResponse>;

public sealed class GetAdminConversationDetailQueryHandler
    : AizenQueryHandler<GetAdminConversationDetailQuery, AdminConversationDetailBffResponse>
{
    private readonly IMessagingAdminBffRemoteCall _remote;
    private readonly IConfiguration _config;

    public GetAdminConversationDetailQueryHandler(
        IMessagingAdminBffRemoteCall remote, IConfiguration config)
    {
        _remote = remote;
        _config = config;
    }

    public override async Task<AdminConversationDetailBffResponse> Handle(
        GetAdminConversationDetailQuery query, CancellationToken ct)
    {
        var response = await _remote.GetConversationDetail(
            query.ConversationId, "Bearer internal", query.UserToken);

        var detail = response.Body
            ?? throw new AizenNotFoundException($"Conversation {query.ConversationId} not found.");

        var hubUrl = _config["Messaging:HubUrl"] ?? "/hubs/messaging";

        return new AdminConversationDetailBffResponse
        {
            Id           = detail.Id,
            Title        = detail.Title,
            ContextType  = detail.ContextType.ToString(),
            ContextId    = detail.ContextId,
            Status       = detail.Status.ToString(),
            HubUrl       = hubUrl,
            Participants = detail.Participants.Select(p => new AdminConversationParticipantDto
            {
                UserId      = p.UserId,
                DisplayName = p.DisplayName,
                Role        = p.Role.ToString()
            }).ToList(),
            Messages = detail.Messages
                .Where(m => !m.IsInternalNote)  // Admin panel sees all — but filter for non-admin BFF consumers
                .Select(MapMessage)
                .ToList()
        };
    }

    private static AdminMessageDto MapMessage(ConversationMessageDto src)
    {
        var dto = new AdminMessageDto
        {
            Id               = src.Id,
            SenderUserId     = src.SenderUserId,
            SenderName       = src.SenderName,
            SenderRole       = src.SenderRole.ToString(),
            Content          = src.Content,
            Type             = src.Type.ToString(),
            IsInternalNote   = src.IsInternalNote,
            ModerationStatus = src.ModerationStatus.ToString(),
            ModerationReason = src.ModerationReason,
            SentAt           = src.SentAt,
            Attachments      = src.Attachments.Select(a => new AdminAttachmentDto
            {
                Id       = a.Id,
                FileName = a.FileName,
                FileType = a.FileType,
                ReadUrl  = a.ReadUrl
            }).ToList()
        };

        // Parse location JSON if applicable
        if (src.Type == MessageType.Location && !string.IsNullOrWhiteSpace(src.Content))
        {
            try
            {
                var loc = JsonSerializer.Deserialize<LocationPayload>(src.Content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (loc is not null)
                    dto.Location = new AdminLocationDto
                    {
                        Lat      = loc.Lat,
                        Lng      = loc.Lng,
                        Label    = loc.Label,
                        Accuracy = loc.Accuracy
                    };
            }
            catch { /* ignore malformed location JSON */ }
        }

        return dto;
    }

    private sealed record LocationPayload(double Lat, double Lng, string Label, double? Accuracy);
}
```

### `GetAdminModerationQueueQuery.cs`
```csharp
public sealed record GetAdminModerationQueueQuery(
    string UserToken, int Skip, int Take
) : AizenQuery<AdminModerationQueueBffResponse>;

public sealed class GetAdminModerationQueueQueryHandler
    : AizenQueryHandler<GetAdminModerationQueueQuery, AdminModerationQueueBffResponse>
{
    private readonly IMessagingAdminBffRemoteCall _remote;

    public GetAdminModerationQueueQueryHandler(IMessagingAdminBffRemoteCall remote)
        => _remote = remote;

    public override async Task<AdminModerationQueueBffResponse> Handle(
        GetAdminModerationQueueQuery query, CancellationToken ct)
    {
        var response = await _remote.GetModerationQueue(
            "Bearer internal", query.UserToken, query.Skip, query.Take);

        var items = response.Body?.Items ?? [];

        return new AdminModerationQueueBffResponse
        {
            Items = items.Select(i => new AdminModerationItemDto
            {
                MessageId         = i.MessageId,
                ConversationId    = i.ConversationId,
                ConversationTitle = i.ConversationTitle,
                SenderName        = i.SenderName,
                SenderRole        = i.SenderRole.ToString(),
                ContentPreview    = i.Content.Length > 120 ? i.Content[..120] + "…" : i.Content,
                ModerationStatus  = i.ModerationStatus.ToString(),
                ModerationReason  = i.ModerationReason,
                SentAt            = i.SentAt
            }).ToList(),
            Total = response.Body?.Total ?? 0
        };
    }
}
```

### `GetAdminMessagingReportQuery.cs`
```csharp
public sealed record GetAdminMessagingReportQuery(
    string UserToken,
    DateTimeOffset? From,
    DateTimeOffset? To
) : AizenQuery<AdminMessagingReportBffResponse>;

public sealed class GetAdminMessagingReportQueryHandler
    : AizenQueryHandler<GetAdminMessagingReportQuery, AdminMessagingReportBffResponse>
{
    private readonly IMessagingAdminBffRemoteCall _remote;

    public GetAdminMessagingReportQueryHandler(IMessagingAdminBffRemoteCall remote)
        => _remote = remote;

    public override async Task<AdminMessagingReportBffResponse> Handle(
        GetAdminMessagingReportQuery query, CancellationToken ct)
    {
        // Parallel fetch
        var providerTask = _remote.GetProviderResponseTime(
            "Bearer internal", query.UserToken, query.From, query.To);
        var channelTask = _remote.GetChannelUsage(
            "Bearer internal", query.UserToken, query.From, query.To);

        await Task.WhenAll(providerTask, channelTask);

        var providerResult = (await providerTask).Body;
        var channelResult  = (await channelTask).Body;

        return new AdminMessagingReportBffResponse
        {
            ProviderResponseTime = (providerResult?.Items ?? []).Select(p => new ProviderResponseTimeBffDto
            {
                ProviderUserId          = p.ProviderUserId,
                ProviderName            = p.ProviderName,
                AvgFirstResponseMinutes = p.AvgFirstResponseMinutes,
                TotalConversations      = p.TotalConversations,
                UnansweredConversations = p.UnansweredConversations
            }).ToList(),
            ChannelUsage = (channelResult?.ByChannel ?? []).Select(c => new ChannelVolumeBffDto
            {
                ContextType                = c.ContextType,
                ConversationCount          = c.ConversationCount,
                MessageCount               = c.MessageCount,
                AvgMessagesPerConversation = c.AvgMessagesPerConversation
            }).ToList(),
            DailyTrend = (channelResult?.DailyTrend ?? []).Select(d => new DailyTrendBffDto
            {
                Date             = d.Date.ToString("yyyy-MM-dd"),
                MessageCount     = d.MessageCount,
                NewConversations = d.NewConversations
            }).ToList(),
            PeakHours = (channelResult?.PeakHours ?? []).Select(h => new PeakHourBffDto
            {
                Hour         = h.Hour,
                MessageCount = h.MessageCount
            }).ToList(),
            GeneratedAt = channelResult?.ReportGeneratedAt ?? DateTimeOffset.UtcNow
        };
    }
}
```

---

## Step 4 — BFF Command Handlers

### `SendAdminMessageCommand.cs`
```csharp
public sealed record SendAdminMessageCommand(
    long ConversationId,
    string Content,
    string Type,
    bool IsInternalNote,
    string? UploadSessionCode,
    string UserToken
) : AizenCommand<SendMessageResponse>;

public sealed class SendAdminMessageCommandHandler
    : AizenCommandHandler<SendAdminMessageCommand, SendMessageResponse>
{
    private readonly IMessagingAdminBffRemoteCall _remote;

    public SendAdminMessageCommandHandler(IMessagingAdminBffRemoteCall remote)
        => _remote = remote;

    public override async Task<SendMessageResponse> Handle(
        SendAdminMessageCommand command, CancellationToken ct)
    {
        var response = await _remote.SendMessage(
            command.ConversationId,
            new SendMessageRequest
            {
                Content           = command.Content,
                Type              = Enum.Parse<MessageType>(command.Type),
                IsInternalNote    = command.IsInternalNote,
                UploadSessionCode = command.UploadSessionCode
            },
            "Bearer internal", command.UserToken);

        return response.Body ?? throw new AizenException("Failed to send message.");
    }
}
```

### `ModerateAdminMessageCommand.cs`
```csharp
public sealed record ModerateAdminMessageCommand(
    long MessageId,
    string Status,
    string? Reason,
    string UserToken
) : AizenCommand<bool>;

public sealed class ModerateAdminMessageCommandHandler
    : AizenCommandHandler<ModerateAdminMessageCommand, bool>
{
    private readonly IMessagingAdminBffRemoteCall _remote;

    public ModerateAdminMessageCommandHandler(IMessagingAdminBffRemoteCall remote)
        => _remote = remote;

    public override async Task<bool> Handle(
        ModerateAdminMessageCommand command, CancellationToken ct)
    {
        var response = await _remote.ModerateMessage(
            command.MessageId,
            new ModerateMessageRequest
            {
                Status = Enum.Parse<MessageModerationStatus>(command.Status),
                Reason = command.Reason
            },
            "Bearer internal", command.UserToken);

        return response.Body;
    }
}
```

---

## Step 5 — BFF Controller

File: `Aizen.Bff.AdminPanel/Controllers/V1/AdminMessagingController.cs`

```csharp
using Aizen.Bff.AdminPanel.Application.AdminMessaging.Dto;
using Aizen.Bff.AdminPanel.Application.AdminMessaging.Query;
using Aizen.Bff.AdminPanel.Application.AdminMessaging.Command;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/messaging")]
[Tags("Admin Panel - Messaging")]
[Authorize(Policy = "AdminPanelAccess")]
[DocumentationInfo("Admin messaging controller",
    "BFF endpoints for admin conversation oversight, moderation, reporting, and message sending.")]
public sealed class AdminMessagingController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminMessagingController(IHttpContextAccessor accessor, IAizenCQRSProcessor cqrs)
        : base(accessor) => _cqrs = cqrs;

    private string UserToken =>
        HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;

    // ── Conversations ─────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/messaging/conversations</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(AdminConversationListBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminConversationListBffResponse?>> GetConversations(
        [FromQuery] string? contextType,
        [FromQuery] string? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminConversationListQuery(UserToken, contextType, status, skip, take), ct);
        return SetResponse(result);
    }

    /// <summary>GET /api/v1/admin-panel/messaging/conversations/{id}</summary>
    [HttpGet("conversations/{id:long}")]
    [ProducesResponseType(typeof(AdminConversationDetailBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminConversationDetailBffResponse?>> GetConversationDetail(
        long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminConversationDetailQuery(id, UserToken), ct);
        return SetResponse(result);
    }

    // ── Messages ──────────────────────────────────────────────────────

    /// <summary>POST /api/v1/admin-panel/messaging/conversations/{id}/messages</summary>
    [HttpPost("conversations/{id:long}/messages")]
    public async Task<AizenApiResponse<SendMessageResponse?>> SendMessage(
        long id,
        [FromBody] AdminSendMessageRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new SendAdminMessageCommand(id, request.Content, request.Type,
                request.IsInternalNote, request.UploadSessionCode, UserToken), ct);
        return SetResponse(result);
    }

    /// <summary>PATCH /api/v1/admin-panel/messaging/conversations/{id}/mark-read</summary>
    [HttpPatch("conversations/{id:long}/mark-read")]
    public async Task<AizenApiResponse<bool?>> MarkRead(
        long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new MarkConversationReadCommand(id, UserToken), ct);
        return SetResponse(result);
    }

    /// <summary>POST /api/v1/admin-panel/messaging/conversations/{id}/attachment-upload-url</summary>
    [HttpPost("conversations/{id:long}/attachment-upload-url")]
    public async Task<AizenApiResponse<RequestAttachmentUploadUrlResponse?>> GetUploadUrl(
        long id,
        [FromBody] AdminAttachmentUploadUrlRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAttachmentUploadUrlCommand(id, request.FileName,
                request.ContentType, request.SizeInBytes, UserToken), ct);
        return SetResponse(result);
    }

    // ── Moderation ────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/messaging/moderation/queue</summary>
    [HttpGet("moderation/queue")]
    [ProducesResponseType(typeof(AdminModerationQueueBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminModerationQueueBffResponse?>> GetModerationQueue(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminModerationQueueQuery(UserToken, skip, take), ct);
        return SetResponse(result);
    }

    /// <summary>PATCH /api/v1/admin-panel/messaging/moderation/messages/{messageId}</summary>
    [HttpPatch("moderation/messages/{messageId:long}")]
    public async Task<AizenApiResponse<bool?>> ModerateMessage(
        long messageId,
        [FromBody] AdminModerateMessageRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new ModerateAdminMessageCommand(messageId, request.Status, request.Reason, UserToken), ct);
        return SetResponse(result);
    }

    /// <summary>PATCH /api/v1/admin-panel/messaging/moderation/conversations/{id}/flag</summary>
    [HttpPatch("moderation/conversations/{id:long}/flag")]
    public async Task<AizenApiResponse<bool?>> FlagConversation(
        long id,
        [FromBody] AdminFlagConversationRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new FlagConversationBffCommand(id, request.Reason, UserToken), ct);
        return SetResponse(result);
    }

    // ── Reporting ─────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/messaging/reports</summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(AdminMessagingReportBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminMessagingReportBffResponse?>> GetReport(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetAdminMessagingReportQuery(UserToken, from, to), ct);
        return SetResponse(result);
    }
}
```

---

## Step 6 — appsettings

Add to BFF's `appsettings.json`:
```json
{
  "RemoteServices": {
    "Messaging": {
      "BaseUrl": "http://localhost:5010"
    }
  },
  "Messaging": {
    "HubUrl": "http://localhost:5010/hubs/messaging"
  }
}
```

Add to BFF's `appsettings.Development.json`:
```json
{
  "RemoteServices": {
    "Messaging": {
      "BaseUrl": "http://localhost:5010"
    }
  },
  "Messaging": {
    "HubUrl": "http://localhost:5010/hubs/messaging"
  }
}
```

## Step 7 — DI Registration

In BFF's `Program.cs` or `DependencyInjection.cs`:

```csharp
// Register Messaging remote call client
builder.Services.AddAizenRemoteCall<IMessagingAdminBffRemoteCall>(
    builder.Configuration["RemoteServices:Messaging:BaseUrl"]
    ?? throw new InvalidOperationException("Messaging base URL not configured."));
```

## Step 8 — Remove Stale SR Conversation Endpoints

In `IServiceRequestAdminBffRemoteCall.cs`, **remove** these two stale methods:
```csharp
// DELETE these — they point to wrong module:
// GetAdminConversations(...)   → /api/v1/messages/conversations
// GetAdminConversationDetail(...) → /api/v1/messages/conversations/{id}
```

Also remove any BFF query/command handlers that reference these methods.

---

## Quality Gates

- [ ] `IMessagingAdminBffRemoteCall` registered in DI — BFF can call Messaging module
- [ ] `GET /api/v1/admin-panel/messaging/conversations` returns list with `hubUrl` field
- [ ] `GET /api/v1/admin-panel/messaging/conversations/{id}` returns detail with `hubUrl = "http://localhost:5010/hubs/messaging"`
- [ ] `Location` type messages have `location.googleMapsUrl` and `location.yandexMapsUrl` populated
- [ ] `MediaAttachment` messages have `attachments[].readUrl` populated (non-null presigned URL)
- [ ] `InternalNote` messages visible in admin detail (admin sees all)
- [ ] Moderation queue returns `pendingFor` field (calculated in BFF)
- [ ] Reports endpoint fetches provider + channel data in parallel (`Task.WhenAll`)
- [ ] Stale SR conversation endpoints removed from `IServiceRequestAdminBffRemoteCall`
- [ ] `dotnet build` BFF project — 0 errors
