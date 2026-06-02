using Aizen.Core.Domain;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Entities.File;

namespace Aizen.Modules.FileStorage.Domain.Entities.Access;

[DocumentationInfo("File access policy entity", "Defines access control rules for a file including visibility, allowed modules and permitted operations.")]
public sealed class FileAccessPolicyEntity : AizenEntityWithAudit
{
    public long FileId { get; private set; }
    public FileVisibility Visibility { get; private set; }
    public string? AllowedOwnerModule { get; private set; }
    public string? AllowedOwnerEntityType { get; private set; }
    public string AllowedOperations { get; private set; } = default!;
    public DateTime? ExpiresAt { get; private set; }

    public FileEntity? File { get; private set; }

    public FileAccessPolicyEntity() { }

    public static FileAccessPolicyEntity Create(
        long fileId, FileVisibility visibility, string? allowedOwnerModule,
        string? allowedOwnerEntityType, string allowedOperations, DateTime? expiresAt)
    {
        return new FileAccessPolicyEntity
        {
            FileId = fileId,
            Visibility = visibility,
            AllowedOwnerModule = allowedOwnerModule,
            AllowedOwnerEntityType = allowedOwnerEntityType,
            AllowedOperations = allowedOperations,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }
}
