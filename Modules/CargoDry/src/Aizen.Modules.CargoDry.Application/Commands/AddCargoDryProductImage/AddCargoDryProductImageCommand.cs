using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.AddCargoDryProductImage;

/// <summary>CargoDry supply flow: registers an uploaded FileStorage image against a product gallery (appended at end).</summary>
public sealed class AddCargoDryProductImageCommand : AizenCommand<CargoDryProductImagesDto>
{
    public string ProductCode { get; init; } = default!;
    public Guid   FileId      { get; init; }
}
