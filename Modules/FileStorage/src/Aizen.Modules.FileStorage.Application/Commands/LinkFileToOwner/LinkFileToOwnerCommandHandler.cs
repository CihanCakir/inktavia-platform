using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Commands.LinkFileToOwner;

[DocumentationInfo("Link file to owner command handler", "Delegates to IFileOwnershipService to link the file, then publishes FileLinkedToOwnerMessage.")]
public sealed class LinkFileToOwnerCommandHandler : AizenCommandHandler<LinkFileToOwnerCommand, FileOwnerReferenceDto>
{
    private readonly IFileOwnershipService _ownershipService;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IAizenInfoAccessor _info;

    public LinkFileToOwnerCommandHandler(IFileOwnershipService ownershipService, IAizenMessagePublisher publisher, IAizenInfoAccessor info)
    {
        _ownershipService = ownershipService;
        _publisher = publisher;
        _info = info;
    }

    public override async Task<FileOwnerReferenceDto?> Handle(LinkFileToOwnerCommand command, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _ownershipService.LinkFileToOwnerAsync(
            command.FileId, command.Request, userId, cancellationToken);

        await _publisher.PublishAsync(new FileLinkedToOwnerMessage
        {
            FileId = result.FileId,
            OwnerModule = result.OwnerModule,
            OwnerEntityType = result.OwnerEntityType,
            OwnerEntityId = result.OwnerEntityId
        }, cancellationToken);

        return result;
    }
}
