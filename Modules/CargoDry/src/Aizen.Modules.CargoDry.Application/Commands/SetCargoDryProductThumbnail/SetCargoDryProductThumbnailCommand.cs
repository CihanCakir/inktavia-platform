using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.SetCargoDryProductThumbnail;

/// <summary>CargoDry supply flow: sets (or clears, when null) the product thumbnail. Non-null must be a gallery image.</summary>
public sealed class SetCargoDryProductThumbnailCommand : AizenCommand<CargoDryProductImagesDto>
{
    public string ProductCode { get; init; } = default!;
    public Guid?  FileId      { get; init; }
}
