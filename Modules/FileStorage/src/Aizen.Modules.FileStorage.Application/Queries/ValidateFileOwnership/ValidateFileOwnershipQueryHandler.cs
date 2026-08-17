using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Application.Queries.ValidateFileOwnership;

[DocumentationInfo("Validate file ownership query handler", "Resolves the file by Guid, delegates to IFileOwnershipService and wraps the result in FileValidationResultDto.")]
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
        var file = await _fileRepository.GetByGuidAsync(request.FileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File not found: {request.FileId}");

        var isValid = await _ownershipService.ValidateOwnershipAsync(
            file.Id,
            request.Request.OwnerModule,
            request.Request.OwnerEntityType,
            request.Request.OwnerEntityId,
            cancellationToken);

        return new FileValidationResultDto
        {
            FileId = file.PublicId ?? Guid.Empty,
            IsValid = isValid,
            Reason = isValid ? null : "File is not linked to the specified owner."
        };
    }
}
