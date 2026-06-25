using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.GenerateBatch;

public sealed class GenerateBatchCommand : AizenCommand<GenerateBatchResultDto>
{
    public string ProductCode { get; init; } = default!;
    public int    Count       { get; init; }
    public long   AdminUserId { get; init; }
}
