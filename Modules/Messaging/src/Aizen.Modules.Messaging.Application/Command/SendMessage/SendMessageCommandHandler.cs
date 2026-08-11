using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Aizen.Modules.Messaging.Application.Realtime;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Messaging.Application.Command.SendMessage;

[DocumentationInfo("Send message command handler",
    "Validates content policy, persists the message, completes file uploads, fires LLM analysis, and broadcasts realtime events.")]
public sealed class SendMessageCommandHandler
    : AizenCommandHandler<SendMessageCommand, SendMessageResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationMessageRepository _messageRepository;
    private readonly IMessageContentPolicy _contentPolicy;
    private readonly IMessagingFileStorageService _fileStorage;
    private readonly MessagingRealtimePublisher _realtimePublisher;
    private readonly IAizenInfoAccessor _info;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IConversationRepository conversationRepository,
        IConversationMessageRepository messageRepository,
        IMessageContentPolicy contentPolicy,
        IMessagingFileStorageService fileStorage,
        MessagingRealtimePublisher realtimePublisher,
        IAizenInfoAccessor info,
        IServiceProvider serviceProvider,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SendMessageCommandHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _messageRepository      = messageRepository;
        _contentPolicy          = contentPolicy;
        _fileStorage            = fileStorage;
        _realtimePublisher      = realtimePublisher;
        _info                   = info;
        _serviceProvider        = serviceProvider;
        _config                 = config;
        _httpContextAccessor    = httpContextAccessor;
        _logger                 = logger;
    }

    public override async Task<SendMessageResponse?> Handle(
        SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {request.ConversationId} not found.");

        if (conversation.Status == ConversationStatus.Closed)
            throw new InvalidOperationException("Cannot send message to a closed conversation.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        // Admins intervene on conversations they don't participate in (W2 internal-note intervention). Read the
        // role from the authenticated principal — the SAME source [Authorize(Roles="Admin")] uses. NB: the admin
        // BFF forwards a service token + X-Aizen-User-Id assertion, and AizenUserInfo.Roles is empty on that
        // assertion path, so UserInfo.Roles is NOT usable here; the service principal still carries the Admin role.
        var isAdmin       = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;

        var participant   = conversation.Participants.FirstOrDefault(p => p.UserId == currentUserId);
        if (participant is null && !isAdmin)
            throw new UnauthorizedAccessException("Sender is not a participant of this conversation.");

        // Non-participant admin → synthesize the sender identity on the message only. The admin is deliberately
        // NOT added as a participant (keeps participant lists + unread counts correct). AizenUserInfo carries no
        // display name, so fall back to "Admin".
        var senderDisplayName = participant?.DisplayName ?? "Admin";
        var senderRole        = participant?.Role ?? MessagingParticipantRole.Admin;

        // BE_WC2 anti-harassment gate — mirrors the SR SendServiceRequestMessage handler: a Provider cannot free-text
        // until the Owner has sent a message in this conversation. Owner / Admin / System / Support sends are ungated.
        if (senderRole == MessagingParticipantRole.Provider
            && !await _messageRepository.HasOwnerMessageAsync(conversation.Id, cancellationToken))
        {
            throw new AizenBusinessException("SR_MSG_CHANNEL_LOCKED");
        }

        // Content moderation — Location type skips text checks
        var policyResult = await _contentPolicy.EvaluateAsync(
            request.Content, currentUserId, request.Type, cancellationToken);

        if (!policyResult.IsAllowed)
        {
            await _realtimePublisher.PublishModerationEventAsync(
                conversation.Id, 0, policyResult.PolicyCode!, policyResult.ViolationReason!, cancellationToken);
            throw new InvalidOperationException(
                $"Message blocked by content policy: {policyResult.ViolationReason}");
        }

        var message = ConversationMessageEntity.Create(
            conversation.Id, currentUserId, senderDisplayName,
            senderRole, request.Content, request.Type, request.IsInternalNote);

        if (policyResult.RequiresReview)
            message.Flag(policyResult.ViolationReason!);

        // BE_WC2 — persist the discrete geo payload onto the WC0 columns for a Location message (parity with the SR
        // write path + the sync mirror). Content still carries the JSON for legacy readers.
        if (request.Type == MessageType.Location)
            message.SetLocation(request.LocationLat, request.LocationLng, request.LocationLabel);

        // Attachment handling — legacy direct FileStorageId path
        if (!string.IsNullOrEmpty(request.AttachmentFileStorageId))
        {
            var attachment = MessageAttachmentEntity.Create(
                0,
                request.AttachmentFileName ?? "attachment",
                request.AttachmentFileType ?? "document",
                request.AttachmentFileStorageId);
            message.AddAttachment(attachment);
        }

        await _messageRepository.AddAsync(message, cancellationToken);
        conversation.AddMessage(message);
        _conversationRepository.Update(conversation);

        // Complete upload session if UploadSessionCode provided (new flow)
        if (request.Type == MessageType.MediaAttachment
            && !string.IsNullOrWhiteSpace(request.UploadSessionCode))
        {
            try
            {
                var fileId = await _fileStorage.CompleteUploadSessionAsync(
                    request.UploadSessionCode, request.Checksum, cancellationToken);

                if (fileId.HasValue && message.Attachments.Any())
                    message.Attachments.First().SetFileStorageId(fileId.Value.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to complete upload session for message {MessageId}.", message.Id);
            }
        }

        if (policyResult.RequiresReview)
            await _realtimePublisher.PublishModerationEventAsync(
                conversation.Id, message.Id, "REVIEW_REQUIRED", policyResult.ViolationReason!, cancellationToken);

        // SAFETY: never broadcast internal admin notes to the participant-facing conversation group /
        // user channels. The MessageSent DTO carries Content + IsInternalNote, so an unconditional publish
        // would leak the note to any participant client that joins messaging:conv:{id}. Mirror the same
        // !IsInternalNote gate used by the Notification bus publish below. Live admin delivery of internal
        // notes is deferred to a later wave (admin-only thin event), not this full-DTO participant broadcast.
        if (!request.IsInternalNote)
        {
            var participantIds = conversation.Participants.Select(p => p.UserId);
            await _realtimePublisher.PublishMessageSentAsync(
                conversation.Id, conversation.ContextId, conversation.ContextType,
                message.ToDto(), participantIds, cancellationToken);
        }

        // Fire-and-forget LLM analysis — only for text messages, non-blocking
        var llmEnabled = _config.GetValue<bool>("Messaging:LlmModeration:Enabled", defaultValue: false);
        if (llmEnabled && request.Type == MessageType.Text && !request.IsInternalNote)
        {
            var savedMessageId  = message.Id;
            var savedContent    = request.Content;
            var savedConvId     = conversation.Id;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope  = _serviceProvider.CreateScope();
                    var analyzer     = scope.ServiceProvider.GetRequiredService<ILlmContentAnalyzer>();
                    var db           = scope.ServiceProvider.GetRequiredService<Aizen.Modules.Messaging.Repository.Persistence.MessagingDbContext>();
                    await analyzer.AnalyzeAsync(savedMessageId, savedContent, savedConvId);
                    await db.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Background LLM analysis scope failed for message {MessageId}.", message.Id);
                }
            }, CancellationToken.None);
        }

        // Publish integration event for Notification module consumers
        if (!request.IsInternalNote)
        {
            var recipientIds = conversation.Participants
                .Where(p => p.UserId != currentUserId)
                .Select(p => p.UserId)
                .ToList();

            if (recipientIds.Count > 0)
            {
                var publisher = _serviceProvider.GetRequiredService<Aizen.Core.Messagebus.Abstraction.Senders.IAizenMessagePublisher>();
                await publisher.PublishAsync(new Aizen.Modules.Messaging.Abstraction.Message.MessagingMessageSentMessage
                {
                    ConversationId    = conversation.Id,
                    ConversationTitle = conversation.Title,
                    SenderUserId      = currentUserId,
                    SenderName        = senderDisplayName,
                    ContextType       = conversation.ContextType,
                    ContextId         = conversation.ContextId,
                    RecipientUserIds  = recipientIds,
                    IsInternalNote    = request.IsInternalNote,
                    SentAt            = message.SentAt,
                }, cancellationToken);
            }
        }

        return new SendMessageResponse(message.ToDto());
    }
}
