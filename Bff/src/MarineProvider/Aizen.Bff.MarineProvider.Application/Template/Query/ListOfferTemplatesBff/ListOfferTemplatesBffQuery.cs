using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Template;

public sealed class ListOfferTemplatesBffQuery : AizenQuery<List<ProviderOfferTemplateDto>>
{
}
