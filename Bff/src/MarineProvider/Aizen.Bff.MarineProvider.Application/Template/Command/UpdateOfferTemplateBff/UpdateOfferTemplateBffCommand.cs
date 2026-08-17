using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

namespace Aizen.Bff.MarineProvider.Application.Template;

public sealed class UpdateOfferTemplateBffCommand : AizenCommand<ProviderOfferTemplateDto>
{
    public long Id { get; init; }
    public OfferTemplateRequest Body { get; init; } = default!;
}
