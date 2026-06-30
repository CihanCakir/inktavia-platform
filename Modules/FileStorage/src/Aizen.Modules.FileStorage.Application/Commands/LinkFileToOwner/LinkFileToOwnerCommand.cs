using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;

namespace Aizen.Modules.FileStorage.Application.Commands.LinkFileToOwner;

[DocumentationInfo("Link file to owner command", "Links an uploaded file to its owning module entity.")]
public sealed class LinkFileToOwnerCommand : AizenCommand<FileOwnerReferenceDto>
{
    public long FileId { get; set; }
    public LinkFileToOwnerRequest Request { get; set; } = default!;
}
