
namespace Aizen.Modules.FileStorage.Abstraction.Request.Access;

[DocumentationInfo("Validate file ownership request", "Checks whether a given module entity owns the specified file.")]
public sealed class ValidateFileOwnershipRequest
{
    public string OwnerModule { get; set; } = default!;
    public string OwnerEntityType { get; set; } = default!;
    public Guid OwnerEntityId { get; set; }
}
