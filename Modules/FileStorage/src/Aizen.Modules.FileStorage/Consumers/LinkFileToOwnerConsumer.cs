using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;
using Aizen.Modules.FileStorage.Application.Commands.LinkFileToOwner;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("Link file to owner consumer", "Request/response consumer that links a file to an owning module entity via CQRS.")]
public sealed class LinkFileToOwnerConsumer
    : AizenBaseMessageConsumer<LinkFileToOwnerProcessMessage, LinkFileToOwnerProcessMessageResult>
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;

    public LinkFileToOwnerConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = serviceProvider.GetRequiredService<IAizenCQRSProcessor>();
    }

    public override Task<bool> ExecutePrepareMessage(
        LinkFileToOwnerProcessMessage message, CancellationToken cancellationToken)
        => Task.FromResult(message.FileId != Guid.Empty);

    public override async Task<LinkFileToOwnerProcessMessageResult> ExecuteCommitMessage(
        LinkFileToOwnerProcessMessage message, CancellationToken cancellationToken)
    {
        var result = await _cqrsProcessor.ProcessAsync<FileOwnerReferenceDto>(
            new LinkFileToOwnerCommand
            {
                FileId = message.FileId,
                Request = new LinkFileToOwnerRequest
                {
                    OwnerModule = message.OwnerModule,
                    OwnerEntityType = message.OwnerEntityType,
                    OwnerEntityId = message.OwnerEntityId
                }
            }, cancellationToken);

        return new LinkFileToOwnerProcessMessageResult
        {
            FileId = message.FileId,
            IsLinked = result is not null
        };
    }

    public override Task ExecuteRollbackMessage(
        LinkFileToOwnerProcessMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
