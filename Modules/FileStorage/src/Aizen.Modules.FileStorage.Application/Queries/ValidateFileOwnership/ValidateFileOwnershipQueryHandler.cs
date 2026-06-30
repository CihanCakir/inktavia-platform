using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Queries.ValidateFileOwnership;

[DocumentationInfo("Validate file ownership query handler", "Delegates to IFileOwnershipService and wraps the result in FileValidationResultDto.")]
public sealed class ValidateFileOwnershipQueryHandler : AizenQueryHandler<ValidateFileOwnershipQuery, FileValidationResultDto>
{
    private readonly IFileOwnershipService _ownershipService;
    private readonly IFileRepository _fileRepository;

    public ValidateFileOwnershipQueryHandler(IFileOwnershipService ownershipService, IFileRepository fileRepository)
    {
        _ownershipService = ownershipService;
        _fileRepository = fileRepository;
    }

    public override async Task<FileValidationResultDto> Handle(ValidateFileOwnershipQuery request, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
        var fileGuid = file?.PublicId ?? Guid.Empty;

        var isValid = await _ownershipService.ValidateOwnershipAsync(
            request.FileId,
            request.Request.OwnerModule,
            request.Request.OwnerEntityType,
            request.Request.OwnerEntityId,
            cancellationToken);

        return new FileValidationResultDto
        {
            FileId = fileGuid,
            IsValid = isValid,
            Reason = isValid ? null : "File is not linked to the specified owner."
        };
    }
}
