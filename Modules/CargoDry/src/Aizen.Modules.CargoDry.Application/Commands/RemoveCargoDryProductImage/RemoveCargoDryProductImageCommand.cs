using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.RemoveCargoDryProductImage;

/// <summary>CargoDry supply flow: removes one image from a product gallery (clears thumbnail if it was that image).</summary>
public sealed class RemoveCargoDryProductImageCommand : AizenCommand<CargoDryProductImagesDto>
{
    public string ProductCode { get; init; } = default!;
    public Guid   FileId      { get; init; }
}
