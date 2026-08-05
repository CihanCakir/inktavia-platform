using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Support;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Abstraction.Response.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

/// <summary>
/// N-D — provider "Canlı destek" (live support). Opens/reads/writes a Messaging-module Support conversation, scoped
/// server-side to the resolved provider. Reuses the Messaging conversation/message/attachment plumbing.
/// </summary>
[ApiController]
[Route("api/v1/provider/support")]
[Tags("Provider - Live Support")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderActive)]
public sealed class ProviderSupportController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderSupportController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    public sealed record CreateSupportBody(SupportTopic Topic, string Subject, string? FirstMessage);
    public sealed record SendSupportBody(long ConversationId, string Content, int Type = 1,
        string? UploadSessionCode = null, string? AttachmentFileStorageId = null,
        string? AttachmentFileName = null, string? AttachmentFileType = null, string? LocationJson = null);
    public sealed record AttachmentUrlBody(long ConversationId, string FileName, string ContentType, long SizeInBytes);

    /// <summary>Open (or reuse) a live-support request for a topic.</summary>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(CreateSupportRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CreateSupportRequestResponse?>> CreateRequest(
        [FromBody] CreateSupportBody body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new CreateProviderSupportRequestCommand
        {
            Topic = body.Topic, Subject = body.Subject, FirstMessage = body.FirstMessage,
        }, ct));

    /// <summary>Read the provider's support thread for a topic (participant-authorized).</summary>
    [HttpGet("thread")]
    [ProducesResponseType(typeof(GetConversationDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetConversationDetailResponse?>> GetThread(
        [FromQuery] SupportTopic topic, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderSupportThreadQuery { Topic = topic }, ct));

    /// <summary>Send a message in a support conversation.</summary>
    [HttpPost("messages")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SendMessageResponse?>> Send(
        [FromBody] SendSupportBody body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SendProviderSupportMessageCommand
        {
            ConversationId = body.ConversationId, Content = body.Content, Type = body.Type,
            UploadSessionCode = body.UploadSessionCode, AttachmentFileStorageId = body.AttachmentFileStorageId,
            AttachmentFileName = body.AttachmentFileName, AttachmentFileType = body.AttachmentFileType,
            LocationJson = body.LocationJson,
        }, ct));

    /// <summary>Request a presigned upload URL for a support attachment.</summary>
    [HttpPost("attachment-upload-url")]
    [ProducesResponseType(typeof(SupportAttachmentUploadUrlBff), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SupportAttachmentUploadUrlBff?>> AttachmentUploadUrl(
        [FromBody] AttachmentUrlBody body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderSupportAttachmentUrlCommand
        {
            ConversationId = body.ConversationId, FileName = body.FileName,
            ContentType = body.ContentType, SizeInBytes = body.SizeInBytes,
        }, ct));
}
