using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Domain.Interface.Service;

[DocumentationInfo("File validation service interface", "Validates content type, extension, file size and checksum rules.")]
public interface IFileValidationService
{
    void ValidateContentType(string contentType);
    void ValidateExtension(string extension);
    void ValidateFileSize(long sizeInBytes, FileCategory category);
    bool ValidateChecksum(string expected, string actual);
    bool IsAllowedContentType(string contentType);
    bool IsAllowedExtension(string extension);
}
