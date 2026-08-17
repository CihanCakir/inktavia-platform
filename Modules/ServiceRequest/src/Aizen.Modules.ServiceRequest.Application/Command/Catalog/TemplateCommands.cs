using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Catalog;

public sealed class CreateTemplateCommand : AizenCommand<ProviderOfferTemplateDto>
{
    public OfferTemplateRequest Request { get; }
    public CreateTemplateCommand(OfferTemplateRequest request) => Request = request;
}

public sealed class UpdateTemplateCommand : AizenCommand<ProviderOfferTemplateDto>
{
    public long TemplateId { get; }
    public OfferTemplateRequest Request { get; }
    public UpdateTemplateCommand(long templateId, OfferTemplateRequest request) { TemplateId = templateId; Request = request; }
}

public sealed class DeleteTemplateCommand : AizenCommand<bool>
{
    public long TemplateId { get; }
    public DeleteTemplateCommand(long templateId) => TemplateId = templateId;
}

public sealed class ListTemplatesQuery : AizenQuery<List<ProviderOfferTemplateDto>> { }

public sealed class GetTemplateQuery : AizenQuery<ProviderOfferTemplateDto>
{
    public long TemplateId { get; }
    public GetTemplateQuery(long templateId) => TemplateId = templateId;
}
