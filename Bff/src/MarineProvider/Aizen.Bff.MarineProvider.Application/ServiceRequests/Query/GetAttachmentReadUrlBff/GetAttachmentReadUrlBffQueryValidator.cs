using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetAttachmentReadUrlBffQueryValidator : AizenValidator<GetAttachmentReadUrlBffQuery>
{
    public GetAttachmentReadUrlBffQueryValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);
        RuleFor(x => x.FileId).NotEmpty();
    }
}
