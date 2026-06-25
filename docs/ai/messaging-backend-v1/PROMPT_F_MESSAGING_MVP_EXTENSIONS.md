# PROMPT F — Messaging Module: MVP Extensions
# FileStorage Integration · Location Messages · LLM Moderation · Reporting

## Overview

This prompt extends the base Messaging module (Prompts A–E) with four MVP-scope features:
1. **FileStorage integration** — presigned upload + read URL via MassTransit message bus
2. **Location messages** — `MessageType.Location = 6`, client-side navigation
3. **LLM async content moderation** — Anthropic API, non-blocking, fire-and-forget
4. **Reporting queries** — Provider response time + Channel usage analytics

**Confirmed message bus pattern from FileStorage module:**
```csharp
// IAizenMessagePublisher — MassTransit request/response
Task<TResponse> SendAsync<TMessage, TResponse>(TMessage message, CancellationToken ct)
    where TMessage : AizenBaseMessage
    where TResponse : class;
```

---

## PART 1 — Enum Updates

### 1.1 Add `Location` to `MessageType`
File: `Aizen.Modules.Messaging.Abstraction/Enum/MessageType.cs`

```csharp
public enum MessageType
{
    Text               = 1,
    SystemNotification = 2,
    StatusChange       = 3,
    InternalNote       = 4,
    MediaAttachment    = 5,
    Location           = 6,   // ← ADD: JSON content { lat, lng, label, accuracy }
}
```

### 1.2 Add `AttachmentUploadRequested` to realtime events
File: `Aizen.Modules.Messaging.Abstraction/Enum/MessagingRealtimeEventType.cs`

```csharp
public enum MessagingRealtimeEventType
{
    MessageSent               = 1,
    MessageModerated          = 2,
    ConversationCreated       = 3,
    ConversationStatusChanged = 4,
    ParticipantJoined         = 5,
    ParticipantLeft           = 6,
    UnreadCountUpdated        = 7,
    AttachmentReady           = 8,   // ← ADD: fires when file processing completes
}
```

---

## PART 2 — FileStorage Integration

### 2.1 Domain Interface
File: `Aizen.Modules.Messaging.Domain/Interface/IMessagingFileStorageService.cs`

```csharp
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Domain.Interface;

[DocumentationInfo("Messaging file storage service interface",
    "Abstraction for FileStorage integration — presigned upload URL generation and read URL resolution.")]
public interface IMessagingFileStorageService
{
    /// <summary>
    /// Creates a presigned S3 upload session via FileStorage message bus.
    /// Returns the upload URL, session code, and file ID for the client.
    /// </summary>
    Task<MessagingUploadSessionResult> CreateUploadSessionAsync(
        string fileName,
        string contentType,
        long sizeInBytes,
        long requestedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Completes an upload session after the client uploads directly to S3.
    /// Returns the confirmed FileStorage Guid for the file.
    /// </summary>
    Task<Guid?> CompleteUploadSessionAsync(
        string uploadSessionCode,
        string? checksum,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a time-limited presigned read URL for an existing file.
    /// Used when enriching message responses with attachment URLs.
    /// </summary>
    Task<string?> GetReadUrlAsync(
        Guid fileStorageId,
        TimeSpan? expiresIn = null,
        CancellationToken ct = default);
}

public sealed record MessagingUploadSessionResult(
    Guid FileId,
    string UploadSessionCode,
    string UploadUrl,
    DateTime ExpiresAt
);
```

### 2.2 Application Service Implementation
File: `Aizen.Modules.Messaging.Application/Services/MessagingFileStorageService.cs`

```csharp
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.Messaging.Domain.Interface;

namespace Aizen.Modules.Messaging.Application.Services;

[DocumentationInfo("Messaging file storage service",
    "Integrates with FileStorage module via MassTransit message bus for presigned upload/read URLs.")]
public sealed class MessagingFileStorageService : IMessagingFileStorageService
{
    private readonly IAizenMessagePublisher _publisher;

    public MessagingFileStorageService(IAizenMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<MessagingUploadSessionResult> CreateUploadSessionAsync(
        string fileName, string contentType, long sizeInBytes,
        long requestedByUserId, CancellationToken ct = default)
    {
        var category = ResolveCategory(contentType);

        var result = await _publisher.SendAsync<
            CreateUploadSessionProcessMessage,
            CreateUploadSessionProcessMessageResult>(
            new CreateUploadSessionProcessMessage
            {
                OriginalFileName    = fileName,
                ContentType         = contentType,
                SizeInBytes         = sizeInBytes,
                Category            = category,
                Visibility          = FileVisibility.Private,
                OwnerModule         = "Messaging",
                OwnerEntityType     = "ConversationMessage",
                OwnerEntityId       = null,   // Linked after message save
                RequestedByUserId   = requestedByUserId
            }, ct);

        return new MessagingUploadSessionResult(
            result.FileId,
            result.UploadSessionCode,
            result.UploadUrl,
            result.ExpiresAt);
    }

    public async Task<Guid?> CompleteUploadSessionAsync(
        string uploadSessionCode, string? checksum, CancellationToken ct = default)
    {
        var result = await _publisher.SendAsync<
            CompleteUploadSessionProcessMessage,
            CompleteUploadSessionProcessMessageResult>(
            new CompleteUploadSessionProcessMessage
            {
                UploadSessionCode = uploadSessionCode,
                Checksum          = checksum
            }, ct);

        return result.IsSuccess ? result.FileId : null;
    }

    public async Task<string?> GetReadUrlAsync(
        Guid fileStorageId, TimeSpan? expiresIn = null, CancellationToken ct = default)
    {
        var result = await _publisher.SendAsync<
            CreateFileReadUrlProcessMessage,
            CreateFileReadUrlProcessMessageResult>(
            new CreateFileReadUrlProcessMessage
            {
                FileId    = fileStorageId,
                ExpiresIn = expiresIn ?? TimeSpan.FromMinutes(30)
            }, ct);

        return string.IsNullOrWhiteSpace(result.ReadUrl) ? null : result.ReadUrl;
    }

    private static FileCategory ResolveCategory(string contentType) => contentType switch
    {
        var ct when ct.StartsWith("image/")       => FileCategory.Image,
        var ct when ct.StartsWith("video/")       => FileCategory.Video,
        "application/pdf"                          => FileCategory.Document,
        _                                          => FileCategory.Other
    };
}
```

### 2.3 New Command: Request Attachment Upload URL
File: `Aizen.Modules.Messaging.Application/Command/RequestAttachmentUploadUrl/`

**`RequestAttachmentUploadUrlCommand.cs`**
```csharp
using Aizen.Core.CQRS.Abstraction;

namespace Aizen.Modules.Messaging.Application.Command.RequestAttachmentUploadUrl;

[DocumentationInfo("Request attachment upload URL command",
    "Creates a presigned S3 upload session for a conversation message attachment.")]
public sealed class RequestAttachmentUploadUrlCommand
    : AizenCommand<RequestAttachmentUploadUrlResponse>
{
    public long ConversationId  { get; init; }
    public string FileName      { get; init; } = default!;
    public string ContentType   { get; init; } = default!;
    public long SizeInBytes     { get; init; }
}

public sealed record RequestAttachmentUploadUrlResponse(
    Guid FileId,
    string UploadSessionCode,
    string UploadUrl,
    DateTime ExpiresAt
);
```

**`RequestAttachmentUploadUrlCommandValidator.cs`**
```csharp
using FluentValidation;

public sealed class RequestAttachmentUploadUrlCommandValidator
    : AbstractValidator<RequestAttachmentUploadUrlCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif",
        "application/pdf",
        "video/mp4", "video/quicktime"
    ];

    private const long MaxImageBytes   = 10L  * 1024 * 1024;  // 10 MB
    private const long MaxVideoBytes   = 100L * 1024 * 1024;  // 100 MB
    private const long MaxDocBytes     = 25L  * 1024 * 1024;  // 25 MB

    public RequestAttachmentUploadUrlCommandValidator()
    {
        RuleFor(x => x.ConversationId).GreaterThan(0);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("File type not allowed. Allowed: jpg, png, webp, gif, pdf, mp4, mov.");
        RuleFor(x => x.SizeInBytes)
            .GreaterThan(0)
            .Must((cmd, size) => size <= GetMaxSize(cmd.ContentType))
            .WithMessage("File size exceeds the limit for this content type.");
    }

    private static long GetMaxSize(string contentType) => contentType switch
    {
        var ct when ct.StartsWith("video/") => MaxVideoBytes,
        var ct when ct.StartsWith("image/") => MaxImageBytes,
        _                                   => MaxDocBytes
    };
}
```

**`RequestAttachmentUploadUrlCommandHandler.cs`**
```csharp
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;

public sealed class RequestAttachmentUploadUrlCommandHandler
    : AizenCommandHandler<RequestAttachmentUploadUrlCommand, RequestAttachmentUploadUrlResponse>
{
    private readonly IConversationRepository _conversations;
    private readonly IMessagingFileStorageService _fileStorage;
    private readonly IAizenInfoAccessor _info;

    public RequestAttachmentUploadUrlCommandHandler(
        IConversationRepository conversations,
        IMessagingFileStorageService fileStorage,
        IAizenInfoAccessor info)
    {
        _conversations = conversations;
        _fileStorage   = fileStorage;
        _info          = info;
    }

    public override async Task<RequestAttachmentUploadUrlResponse> Handle(
        RequestAttachmentUploadUrlCommand command, CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;

        var conversation = await _conversations.GetByIdAsync(command.ConversationId, ct)
            ?? throw new AizenNotFoundException($"Conversation {command.ConversationId} not found.");

        // Verify caller is a participant
        var isParticipant = conversation.Participants.Any(p => p.UserId == userId && p.IsActive);
        if (!isParticipant)
            throw new AizenForbiddenException("You are not a participant of this conversation.");

        var session = await _fileStorage.CreateUploadSessionAsync(
            command.FileName, command.ContentType, command.SizeInBytes, userId, ct);

        return new RequestAttachmentUploadUrlResponse(
            session.FileId,
            session.UploadSessionCode,
            session.UploadUrl,
            session.ExpiresAt);
    }
}
```

### 2.4 Update `SendMessageCommand` — Complete Upload on Send

In `SendMessageCommandHandler`, after the message entity is saved:

```csharp
// If message has MediaAttachment type and UploadSessionCode is provided → complete the upload
if (command.Type == MessageType.MediaAttachment
    && !string.IsNullOrWhiteSpace(command.UploadSessionCode))
{
    var fileId = await _fileStorage.CompleteUploadSessionAsync(
        command.UploadSessionCode, command.Checksum, ct);

    if (fileId.HasValue && message.Attachments.Any())
    {
        // FileStorageId is set on the attachment entity
        // Update the already-added attachment with the resolved Guid
        // (attachment was created with temp fileStorageId = null)
        var attachment = message.Attachments.First();
        attachment.SetFileStorageId(fileId.Value.ToString());
    }
}
```

Add `SetFileStorageId(string id)` domain method to `MessageAttachmentEntity`:
```csharp
public void SetFileStorageId(string fileStorageId)
    => FileStorageId = fileStorageId;
```

Also update `SendMessageCommand` to add:
```csharp
public string? UploadSessionCode { get; init; }
public string? Checksum          { get; init; }
```

### 2.5 Read URL Enrichment in `GetConversationDetailQueryHandler`

After loading messages, enrich attachments with read URLs:

```csharp
// In GetConversationDetailQueryHandler.Handle():
foreach (var message in conversation.Messages
    .Where(m => m.Type == MessageType.MediaAttachment))
{
    foreach (var attachment in message.Attachments
        .Where(a => !string.IsNullOrWhiteSpace(a.FileStorageId)))
    {
        if (Guid.TryParse(attachment.FileStorageId, out var fileGuid))
        {
            var readUrl = await _fileStorage.GetReadUrlAsync(fileGuid, expiresIn: TimeSpan.FromMinutes(30), ct);
            // Include readUrl in DTO mapping
            // Add ReadUrl to AttachmentDto
        }
    }
}
```

Add `ReadUrl` to `AttachmentDto` in Abstraction:
```csharp
public sealed record AttachmentDto(
    long Id,
    string FileName,
    string FileType,
    string? FileStorageId,
    string? ReadUrl   // ← ADD: populated at query time
);
```

### 2.6 New API Endpoint

Add to `ConversationMessagesController`:

```csharp
/// <summary>
/// Request a presigned S3 upload URL for a conversation attachment.
/// Client uploads directly to S3, then calls SendMessage with UploadSessionCode.
/// POST /api/v1/conversations/{conversationId}/messages/attachment-upload-url
/// </summary>
[HttpPost("attachment-upload-url")]
[ProducesResponseType(typeof(RequestAttachmentUploadUrlResponse), StatusCodes.Status200OK)]
public async Task<AizenApiResponse<RequestAttachmentUploadUrlResponse?>> GetAttachmentUploadUrl(
    [FromRoute] long conversationId,
    [FromBody] AttachmentUploadUrlRequest request,
    CancellationToken ct = default)
{
    var result = await _cqrs.ProcessAsync<RequestAttachmentUploadUrlResponse>(
        new RequestAttachmentUploadUrlCommand
        {
            ConversationId = conversationId,
            FileName       = request.FileName,
            ContentType    = request.ContentType,
            SizeInBytes    = request.SizeInBytes
        }, ct);
    return SetResponse(result);
}
```

---

## PART 3 — Location Messages

### 3.1 Location Content Contract (no new entity needed)

When `MessageType.Location`, the `Content` field carries JSON:
```json
{
  "lat": 43.6900,
  "lng": 7.2658,
  "label": "Antibes Port Vauban, Berth J12",
  "accuracy": 15
}
```

### 3.2 Location Content DTO
File: `Aizen.Modules.Messaging.Abstraction/Response/Conversation/LocationContentDto.cs`

```csharp
namespace Aizen.Modules.Messaging.Abstraction.Response.Conversation;

[DocumentationInfo("Location content DTO", "Parsed location payload for MessageType.Location messages.")]
public sealed record LocationContentDto(
    double Lat,
    double Lng,
    string Label,
    double? Accuracy
)
{
    /// <summary>Google Maps URL — opened by web clients and as fallback.</summary>
    public string GoogleMapsUrl
        => $"https://www.google.com/maps?q={Lat},{Lng}";

    /// <summary>Yandex Maps URL — preferred for TR/RU clients.</summary>
    public string YandexMapsUrl
        => $"https://yandex.com/maps/?pt={Lng},{Lat}&z=16&l=map";

    /// <summary>Universal geo URI — iOS/Android opens native map app.</summary>
    public string GeoUri
        => $"geo:{Lat},{Lng}?q={Lat},{Lng}({Uri.EscapeDataString(Label)})";
}
```

### 3.3 Content Policy — Skip URL Check for Location

In `MessageContentPolicyService`, add a guard before URL count check:

```csharp
// Skip URL and off-platform checks for Location messages
// (Location JSON contains no user-authored text)
// The caller passes MessageType as part of the evaluate context

// Update interface signature:
Task<ContentPolicyResult> EvaluateAsync(
    string content,
    long senderUserId,
    MessageType messageType = MessageType.Text,
    CancellationToken ct = default);

// In implementation:
if (messageType == MessageType.Location)
{
    // Only validate JSON structure
    try { JsonDocument.Parse(content); }
    catch { return ContentPolicyResult.Block("Invalid location payload.", "INVALID_LOCATION"); }
    return ContentPolicyResult.Allow();
}
```

### 3.4 `SendMessageCommand` Updates

Add `LocationContent` property to request:
```csharp
// In SendMessageRequest (Abstraction):
public string? LocationJson { get; init; }  // Only set when Type = Location

// Validator:
When(x => x.Type == MessageType.Location, () => {
    RuleFor(x => x.LocationJson)
        .NotEmpty()
        .Must(json => {
            try { JsonDocument.Parse(json!); return true; }
            catch { return false; }
        })
        .WithMessage("LocationJson must be valid JSON when MessageType is Location.");
});
```

---

## PART 4 — LLM Async Content Moderation

### 4.1 Domain Interface
File: `Aizen.Modules.Messaging.Domain/Interface/ILlmContentAnalyzer.cs`

```csharp
namespace Aizen.Modules.Messaging.Domain.Interface;

[DocumentationInfo("LLM content analyzer interface",
    "Asynchronous AI-powered content analysis — runs after message delivery, non-blocking.")]
public interface ILlmContentAnalyzer
{
    /// <summary>
    /// Analyzes message content using LLM. Fire-and-forget from the send pipeline.
    /// Mutates message moderation status if risk is detected.
    /// </summary>
    Task AnalyzeAsync(long messageId, string content, long conversationId, CancellationToken ct = default);
}
```

### 4.2 Application Service Implementation
File: `Aizen.Modules.Messaging.Application/Services/AnthropicLlmContentAnalyzer.cs`

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Messaging.Application.Services;

[DocumentationInfo("Anthropic LLM content analyzer",
    "Calls Claude API to detect nuanced policy violations: tacit off-platform solicitation, threats, competitor referrals.")]
public sealed class AnthropicLlmContentAnalyzer : ILlmContentAnalyzer
{
    private readonly HttpClient _http;
    private readonly IConversationMessageRepository _messages;
    private readonly IConversationRepository _conversations;
    private readonly ILogger<AnthropicLlmContentAnalyzer> _logger;
    private readonly string _apiKey;

    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-haiku-4-5-20251001";  // Fast + cheap for moderation

    public AnthropicLlmContentAnalyzer(
        IHttpClientFactory httpFactory,
        IConversationMessageRepository messages,
        IConversationRepository conversations,
        IConfiguration config,
        ILogger<AnthropicLlmContentAnalyzer> logger)
    {
        _http          = httpFactory.CreateClient("anthropic");
        _messages      = messages;
        _conversations = conversations;
        _logger        = logger;
        _apiKey        = config["Messaging:LlmModeration:AnthropicApiKey"]
                         ?? throw new InvalidOperationException("Anthropic API key not configured.");
    }

    public async Task AnalyzeAsync(long messageId, string content, long conversationId, CancellationToken ct = default)
    {
        try
        {
            var conversation = await _conversations.GetByIdAsync(conversationId, ct);
            var contextLabel = conversation?.ContextType.ToString() ?? "Unknown";

            var prompt = BuildPrompt(content, contextLabel);
            var verdict = await CallAnthropicAsync(prompt, ct);

            if (verdict is null || verdict.Risk == "none" || verdict.Risk == "low")
                return;

            var message = await _messages.GetByIdAsync(messageId, ct);
            if (message is null) return;

            var newStatus = verdict.Risk == "high"
                ? MessageModerationStatus.Flagged
                : MessageModerationStatus.PendingReview;

            message.SetModerationStatus(newStatus, $"LLM: {verdict.Reason}");
            _messages.Update(message);

            // Note: SaveChanges must be called by the caller or a scoped UoW
            // Use IUnitOfWork or DbContext.SaveChangesAsync here
        }
        catch (Exception ex)
        {
            // Silent fail — LLM moderation is non-critical
            _logger.LogWarning(ex, "LLM content analysis failed for message {MessageId}. Continuing.", messageId);
        }
    }

    private static string BuildPrompt(string content, string contextType) => $"""
        You are a content moderator for a professional marine marketplace platform.
        Analyze the following message and return a JSON verdict.

        Context: {contextType} conversation between marine service professionals.

        Message: "{content}"

        Check for:
        - Off-platform payment or communication solicitation (tacit or obfuscated)
        - Harassment, threats, or abusive language
        - Competitor platform referrals or price undercutting schemes
        - Fraud signals (fake identity, false credentials)

        Respond ONLY with valid JSON in this format:
        {{ "risk": "none|low|medium|high", "reason": "brief explanation or empty string" }}
        """;

    private async Task<LlmVerdict?> CallAnthropicAsync(string prompt, CancellationToken ct)
    {
        var request = new
        {
            model = Model,
            max_tokens = 128,
            messages = new[] { new { role = "user", content = prompt } }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl);
        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(httpRequest, ct);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: ct);
        var text = body?.Content?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Strip markdown fences if present
        text = text.Trim().TrimStart('`').TrimEnd('`').Trim();
        if (text.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            text = text[4..].Trim();

        return JsonSerializer.Deserialize<LlmVerdict>(text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private sealed record LlmVerdict(string Risk, string Reason);

    private sealed record AnthropicResponse(
        List<AnthropicContentBlock>? Content);

    private sealed record AnthropicContentBlock(string Type, string Text);
}
```

### 4.3 Fire-and-Forget in `SendMessageCommandHandler`

After `SaveChangesAsync`, add:

```csharp
// Fire-and-forget LLM analysis — non-blocking
if (command.Type == MessageType.Text && !command.IsInternalNote)
{
    _ = Task.Run(async () =>
    {
        using var scope = _serviceProvider.CreateScope();
        var analyzer = scope.ServiceProvider.GetRequiredService<ILlmContentAnalyzer>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await analyzer.AnalyzeAsync(savedMessageId, command.Content, command.ConversationId);
        await uow.SaveChangesAsync();
    }, CancellationToken.None);
}
```

Inject `IServiceProvider` in handler constructor. Always create a new scope for the background task (EF context is not thread-safe).

### 4.4 HttpClient Registration in `Program.cs`

```csharp
builder.Services.AddHttpClient("anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.Timeout = TimeSpan.FromSeconds(10);
});
```

### 4.5 appsettings addition

```json
{
  "Messaging": {
    "LlmModeration": {
      "AnthropicApiKey": "sk-ant-...",
      "Enabled": true
    }
  }
}
```

Add guard in `SendMessageCommandHandler`:
```csharp
var llmEnabled = _config.GetValue<bool>("Messaging:LlmModeration:Enabled", defaultValue: false);
if (llmEnabled && command.Type == MessageType.Text && !command.IsInternalNote)
{ /* fire-and-forget */ }
```

---

## PART 5 — Reporting Queries (MVP)

### 5.1 Provider Response Time Query
File: `Aizen.Modules.Messaging.Application/Query/GetProviderResponseTimeReport/`

**`GetProviderResponseTimeReportQuery.cs`**
```csharp
using Aizen.Core.CQRS.Abstraction;

[DocumentationInfo("Get provider response time report query", "Aggregates first-response time per provider for ServiceRequest conversations.")]
public sealed class GetProviderResponseTimeReportQuery
    : AizenQuery<GetProviderResponseTimeReportResponse>
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To   { get; init; }
    public int Take             { get; init; } = 20;
}

public sealed record GetProviderResponseTimeReportResponse(
    List<ProviderResponseTimeDto> Items,
    DateTimeOffset ReportGeneratedAt
);

public sealed record ProviderResponseTimeDto(
    long ProviderUserId,
    string ProviderName,
    double AvgFirstResponseMinutes,
    int TotalConversations,
    int UnansweredConversations   // Provider never replied
);
```

**`GetProviderResponseTimeReportQueryHandler.cs`**
```csharp
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class GetProviderResponseTimeReportQueryHandler
    : AizenQueryHandler<GetProviderResponseTimeReportQuery, GetProviderResponseTimeReportResponse>
{
    private readonly MessagingDbContext _db;

    public GetProviderResponseTimeReportQueryHandler(MessagingDbContext db) => _db = db;

    public override async Task<GetProviderResponseTimeReportResponse> Handle(
        GetProviderResponseTimeReportQuery query, CancellationToken ct)
    {
        // Only SR conversations
        var conversationsQuery = _db.Conversations
            .AsNoTracking()
            .Where(c => c.ContextType == MessagingContextType.ServiceRequest && !c.IsDeleted);

        if (query.From.HasValue) conversationsQuery = conversationsQuery.Where(c => c.CreatedAt >= query.From.Value);
        if (query.To.HasValue)   conversationsQuery = conversationsQuery.Where(c => c.CreatedAt <= query.To.Value);

        var conversations = await conversationsQuery
            .Select(c => new
            {
                c.Id,
                FirstOwnerMessageAt = _db.ConversationMessages
                    .Where(m => m.ConversationId == c.Id
                             && m.SenderRole == MessagingParticipantRole.Owner
                             && !m.IsDeleted)
                    .OrderBy(m => m.SentAt)
                    .Select(m => (DateTimeOffset?)m.SentAt)
                    .FirstOrDefault(),
                FirstProviderMessageAt = _db.ConversationMessages
                    .Where(m => m.ConversationId == c.Id
                             && m.SenderRole == MessagingParticipantRole.Provider
                             && !m.IsDeleted)
                    .OrderBy(m => m.SentAt)
                    .Select(m => (DateTimeOffset?)m.SentAt)
                    .FirstOrDefault(),
                ProviderUserId = _db.ConversationParticipants
                    .Where(p => p.ConversationId == c.Id
                             && p.Role == MessagingParticipantRole.Provider)
                    .Select(p => (long?)p.UserId)
                    .FirstOrDefault(),
                ProviderName = _db.ConversationParticipants
                    .Where(p => p.ConversationId == c.Id
                             && p.Role == MessagingParticipantRole.Provider)
                    .Select(p => p.DisplayName)
                    .FirstOrDefault()
            })
            .Where(x => x.ProviderUserId != null)
            .ToListAsync(ct);

        var grouped = conversations
            .GroupBy(x => new { x.ProviderUserId, x.ProviderName })
            .Select(g =>
            {
                var answered = g.Where(x =>
                    x.FirstOwnerMessageAt.HasValue &&
                    x.FirstProviderMessageAt.HasValue &&
                    x.FirstProviderMessageAt > x.FirstOwnerMessageAt).ToList();

                var avgMinutes = answered.Any()
                    ? answered.Average(x =>
                        (x.FirstProviderMessageAt!.Value - x.FirstOwnerMessageAt!.Value).TotalMinutes)
                    : 0;

                return new ProviderResponseTimeDto(
                    g.Key.ProviderUserId!.Value,
                    g.Key.ProviderName ?? "Unknown",
                    Math.Round(avgMinutes, 1),
                    g.Count(),
                    g.Count() - answered.Count);
            })
            .OrderBy(x => x.AvgFirstResponseMinutes)
            .Take(query.Take)
            .ToList();

        return new GetProviderResponseTimeReportResponse(grouped, DateTimeOffset.UtcNow);
    }
}
```

---

### 5.2 Channel Usage Analytics Query
File: `Aizen.Modules.Messaging.Application/Query/GetChannelUsageReport/`

**`GetChannelUsageReportQuery.cs`**
```csharp
[DocumentationInfo("Get channel usage report query", "Message volume by context type, daily trend, and peak hour breakdown.")]
public sealed class GetChannelUsageReportQuery : AizenQuery<GetChannelUsageReportResponse>
{
    public DateTimeOffset From { get; init; } = DateTimeOffset.UtcNow.AddDays(-30);
    public DateTimeOffset To   { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record GetChannelUsageReportResponse(
    List<ChannelVolumeDto> ByChannel,
    List<DailyMessageVolumeDto> DailyTrend,
    List<PeakHourDto> PeakHours,
    DateTimeOffset ReportGeneratedAt
);

public sealed record ChannelVolumeDto(
    string ContextType,
    int ConversationCount,
    int MessageCount,
    double AvgMessagesPerConversation
);

public sealed record DailyMessageVolumeDto(
    DateOnly Date,
    int MessageCount,
    int NewConversations
);

public sealed record PeakHourDto(int Hour, int MessageCount);
```

**`GetChannelUsageReportQueryHandler.cs`**
```csharp
public sealed class GetChannelUsageReportQueryHandler
    : AizenQueryHandler<GetChannelUsageReportQuery, GetChannelUsageReportResponse>
{
    private readonly MessagingDbContext _db;

    public GetChannelUsageReportQueryHandler(MessagingDbContext db) => _db = db;

    public override async Task<GetChannelUsageReportResponse> Handle(
        GetChannelUsageReportQuery query, CancellationToken ct)
    {
        var from = query.From;
        var to   = query.To;

        // Channel volume
        var byChannel = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.CreatedAt >= from && c.CreatedAt <= to && !c.IsDeleted)
            .GroupBy(c => c.ContextType)
            .Select(g => new
            {
                ContextType = g.Key.ToString(),
                ConversationCount = g.Count(),
                MessageCount = _db.ConversationMessages
                    .Count(m => g.Select(c => c.Id).Contains(m.ConversationId) && !m.IsDeleted)
            })
            .ToListAsync(ct);

        var channelDtos = byChannel.Select(x => new ChannelVolumeDto(
            x.ContextType,
            x.ConversationCount,
            x.MessageCount,
            x.ConversationCount > 0
                ? Math.Round((double)x.MessageCount / x.ConversationCount, 1)
                : 0
        )).ToList();

        // Daily trend
        var dailyTrend = await _db.ConversationMessages
            .AsNoTracking()
            .Where(m => m.SentAt >= from && m.SentAt <= to && !m.IsDeleted)
            .GroupBy(m => m.SentAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyMessageVolumeDto(
                DateOnly.FromDateTime(g.Key),
                g.Count(),
                _db.Conversations.Count(c =>
                    c.CreatedAt.Date == g.Key && !c.IsDeleted)
            ))
            .ToListAsync(ct);

        // Peak hours (UTC)
        var peakHours = await _db.ConversationMessages
            .AsNoTracking()
            .Where(m => m.SentAt >= from && m.SentAt <= to && !m.IsDeleted)
            .GroupBy(m => m.SentAt.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new PeakHourDto(g.Key, g.Count()))
            .ToListAsync(ct);

        return new GetChannelUsageReportResponse(
            channelDtos, dailyTrend, peakHours, DateTimeOffset.UtcNow);
    }
}
```

### 5.3 Reporting Controller
File: `Aizen.Modules.Messaging/Controller/V1/Reporting/MessagingReportingController.cs`

```csharp
[ApiController]
[Route("api/v1/reporting/messaging")]
[Tags("Messaging - Reporting")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Messaging reporting controller",
    "Admin-only reporting endpoints: provider response time and channel usage analytics.")]
public sealed class MessagingReportingController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MessagingReportingController(IHttpContextAccessor accessor, IAizenCQRSProcessor cqrs)
        : base(accessor) => _cqrs = cqrs;

    /// <summary>
    /// Provider first-response time report.
    /// GET /api/v1/reporting/messaging/provider-response-time
    /// </summary>
    [HttpGet("provider-response-time")]
    public async Task<AizenApiResponse<GetProviderResponseTimeReportResponse?>> ProviderResponseTime(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetProviderResponseTimeReportResponse>(
            new GetProviderResponseTimeReportQuery { From = from, To = to, Take = take }, ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Channel usage analytics (last 30 days by default).
    /// GET /api/v1/reporting/messaging/channel-usage
    /// </summary>
    [HttpGet("channel-usage")]
    public async Task<AizenApiResponse<GetChannelUsageReportResponse?>> ChannelUsage(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetChannelUsageReportResponse>(
            new GetChannelUsageReportQuery
            {
                From = from ?? DateTimeOffset.UtcNow.AddDays(-30),
                To   = to   ?? DateTimeOffset.UtcNow
            }, ct);
        return SetResponse(result);
    }
}
```

---

## PART 6 — DI Registration Updates

Update `DependencyInjection.cs → AddMessagingServices()`:

```csharp
public static IServiceCollection AddMessagingServices(this IServiceCollection services)
{
    services.AddScoped<IMessagingFileStorageService, MessagingFileStorageService>();
    services.AddScoped<ILlmContentAnalyzer, AnthropicLlmContentAnalyzer>();
    return services;
}
```

Update `Program.cs` — add HttpClient for Anthropic:

```csharp
builder.Services.AddHttpClient("anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.Timeout = TimeSpan.FromSeconds(10);
});
```

---

## Quality Gates

- [ ] `CreateUploadSessionProcessMessage` sends correctly — `OwnerModule = "Messaging"`, `OwnerEntityType = "ConversationMessage"`
- [ ] `CompleteUploadSessionProcessMessage` called only when `UploadSessionCode` present
- [ ] `GetReadUrlAsync` enriches all `MediaAttachment` messages in detail query
- [ ] `MessageType.Location = 6` passes content policy with JSON-only validation
- [ ] `LocationContentDto.GoogleMapsUrl`, `.YandexMapsUrl`, `.GeoUri` produce correct URLs for given coordinates
- [ ] LLM analyzer silent-fails when Anthropic API is down — message still delivered
- [ ] LLM fire-and-forget uses new DI scope — no EF concurrency errors
- [ ] `Messaging:LlmModeration:Enabled = false` disables LLM without code change
- [ ] Provider response time report returns correct avg for test data (SR 9001: Aria Voss, SR 9004: Nico Hartmann)
- [ ] Channel usage query returns correct counts for seeded 3 conversations / 12 messages
- [ ] Reporting controller returns 403 for non-Admin caller
