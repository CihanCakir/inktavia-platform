
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.System;

/// <summary>Seed model for a Language entity, read from languages.json.</summary>
[DocumentationInfo("Seed model representing a platform language loaded from JSON.", "Maps to LanguageEntity. Idempotency key: Code.")]
public sealed class LanguageSeedModel
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string NativeName { get; set; } = default!;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
