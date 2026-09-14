using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.ReorderCargoDryProductImages;

/// <summary>CargoDry supply flow: reorders a product's gallery to match the supplied file-id permutation.</summary>
public sealed class ReorderCargoDryProductImagesCommand : AizenCommand<CargoDryProductImagesDto>
{
    public string      ProductCode    { get; init; } = default!;
    public List<Guid>  OrderedFileIds { get; init; } = new();
}
