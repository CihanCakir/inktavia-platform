using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Template;

public sealed class GetOfferTemplateBffQuery : AizenQuery<ProviderOfferTemplateDto>
{
    public long Id { get; init; }
}
