using Aizen.Core.Validation;
using Aizen.Modules.FileStorage.Abstraction.Model;
using FluentValidation;

namespace Aizen.Modules.FileStorage.Application.Commands.CreateUploadSession;

[DocumentationInfo("Create upload session command validator", "Validates that the upload session request contains a file name, content type and a positive size.")]
public sealed class CreateUploadSessionCommandValidator : AizenValidator<CreateUploadSessionCommand>
{
    public CreateUploadSessionCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.OriginalFileName).NotEmpty().When(x => x.Request != null);
        RuleFor(x => x.Request.ContentType).NotEmpty().When(x => x.Request != null);
        RuleFor(x => x.Request.SizeInBytes).GreaterThan(0).When(x => x.Request != null);
    }
}
