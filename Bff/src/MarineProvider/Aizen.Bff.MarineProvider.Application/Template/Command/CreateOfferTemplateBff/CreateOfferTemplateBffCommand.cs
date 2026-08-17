using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

namespace Aizen.Bff.MarineProvider.Application.Template;

public sealed class CreateOfferTemplateBffCommand : AizenCommand<ProviderOfferTemplateDto>
{
    public OfferTemplateRequest Body { get; init; } = default!;
}
