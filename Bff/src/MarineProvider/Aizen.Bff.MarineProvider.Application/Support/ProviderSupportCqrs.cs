using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Request.Messaging;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;

namespace Aizen.Bff.MarineProvider.Application.Support;

// ─── N-D provider live-support BFF (create / read thread / send / attachment) ──────────────────────────────────
// Every handler resolves the provider identity server-side; the Messaging module authorizes the participant from the
// asserted X-Aizen-User-Id (no user id crosses the wire). ContextId is derived (userId*100 + topic) so the FE only
// ever deals in a topic, never the internal conversation-context id.

// ---- Create / reuse a support request -------------------------------------------------------------------------
public sealed class CreateProviderSupportRequestCommand : AizenCommand<CreateSupportRequestResponse>
{
    public SupportTopic Topic        { get; init; }
    public string       Subject      { get; init; } = default!;
    public string?      FirstMessage { get; init; }
}

public sealed class CreateProviderSupportRequestCommandHandler
    : AizenCommandHandler<CreateProviderSupportRequestCommand, CreateSupportRequestResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder  _holder;
    private readonly IProviderContext         _context;
    private readonly IMessagingRemoteCall     _messaging;

    public CreateProviderSupportRequestCommandHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder holder,
        IProviderContext context, IMessagingRemoteCall messaging)
    { _resolver = resolver; _holder = holder; _context = context; _messaging = messaging; }

    public override async Task<CreateSupportRequestResponse?> Handle(
        CreateProviderSupportRequestCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_holder.UserId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var name = ProviderSupportHelpers.DisplayName(_context);
        return (await _messaging.CreateSupportRequest(new CreateSupportRequestRequest(
            request.Topic, request.Subject, request.FirstMessage, name, MessagingParticipantRole.Provider))).Body;
    }
}

// ---- Read the provider's support thread for a topic -----------------------------------------------------------
public sealed class GetProviderSupportThreadQuery : AizenQuery<GetConversationDetailResponse>
{
    public SupportTopic Topic { get; init; }
}

public sealed class GetProviderSupportThreadQueryHandler
    : AizenQueryHandler<GetProviderSupportThreadQuery, GetConversationDetailResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder  _holder;
    private readonly IMessagingRemoteCall     _messaging;

    public GetProviderSupportThreadQueryHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder holder, IMessagingRemoteCall messaging)
    { _resolver = resolver; _holder = holder; _messaging = messaging; }

    public override async Task<GetConversationDetailResponse?> Handle(
        GetProviderSupportThreadQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_holder.UserId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var contextId = _holder.UserId.Value * 100 + (int)request.Topic;
        return (await _messaging.GetMyConversationByContext(MessagingContextType.Support, contextId)).Body;
    }
}

// ---- Send a message in a support conversation -----------------------------------------------------------------
public sealed class SendProviderSupportMessageCommand : AizenCommand<SendMessageResponse>
{
    public long    ConversationId          { get; init; }
    public string  Content                 { get; init; } = string.Empty;
    public int     Type                    { get; init; } = 1; // MessageType.Text
    public string? AttachmentFileStorageId { get; init; }
    public string? AttachmentFileName      { get; init; }
    public string? AttachmentFileType      { get; init; }
    public string? UploadSessionCode       { get; init; }
    public string? LocationJson            { get; init; }
}

public sealed class SendProviderSupportMessageCommandHandler
    : AizenCommandHandler<SendProviderSupportMessageCommand, SendMessageResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder  _holder;
    private readonly IMessagingRemoteCall     _messaging;

    public SendProviderSupportMessageCommandHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder holder, IMessagingRemoteCall messaging)
    { _resolver = resolver; _holder = holder; _messaging = messaging; }

    public override async Task<SendMessageResponse?> Handle(
        SendProviderSupportMessageCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_holder.UserId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        return (await _messaging.SendMessage(request.ConversationId, new SendMessageRequest(
            request.Content, (MessageType)request.Type, IsInternalNote: false,
            AttachmentFileStorageId: request.AttachmentFileStorageId,
            AttachmentFileName: request.AttachmentFileName,
            AttachmentFileType: request.AttachmentFileType,
            UploadSessionCode: request.UploadSessionCode,
            Checksum: null,
            LocationJson: request.LocationJson))).Body;
    }
}

// ---- Attachment upload url in a support conversation ----------------------------------------------------------
public sealed class GetProviderSupportAttachmentUrlCommand : AizenCommand<SupportAttachmentUploadUrlBff>
{
    public long   ConversationId { get; init; }
    public string FileName       { get; init; } = default!;
    public string ContentType    { get; init; } = default!;
    public long   SizeInBytes    { get; init; }
}

public sealed class GetProviderSupportAttachmentUrlCommandHandler
    : AizenCommandHandler<GetProviderSupportAttachmentUrlCommand, SupportAttachmentUploadUrlBff>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder  _holder;
    private readonly IMessagingRemoteCall     _messaging;

    public GetProviderSupportAttachmentUrlCommandHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder holder, IMessagingRemoteCall messaging)
    { _resolver = resolver; _holder = holder; _messaging = messaging; }

    public override async Task<SupportAttachmentUploadUrlBff?> Handle(
        GetProviderSupportAttachmentUrlCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_holder.UserId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        return (await _messaging.GetAttachmentUploadUrl(request.ConversationId, new AttachmentUploadUrlRequest(
            request.FileName, request.ContentType, request.SizeInBytes))).Body;
    }
}

internal static class ProviderSupportHelpers
{
    public static string DisplayName(IProviderContext ctx)
    {
        var full = $"{ctx.FirstName} {ctx.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(full)) return full;
        return ctx.PreferredUsername ?? ctx.Email ?? "Sağlayıcı";
    }
}
