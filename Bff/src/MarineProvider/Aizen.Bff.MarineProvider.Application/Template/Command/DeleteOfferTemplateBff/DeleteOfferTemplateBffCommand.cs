using Aizen.Bff.MarineProvider.Application.Common;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Template;

public sealed class DeleteOfferTemplateBffCommand : AizenCommand<BffSuccessResult>
{
    public long Id { get; init; }
}
