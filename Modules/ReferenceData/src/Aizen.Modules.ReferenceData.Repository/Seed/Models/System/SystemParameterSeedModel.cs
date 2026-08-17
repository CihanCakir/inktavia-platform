
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.System;

/// <summary>Seed model for a SystemParameter entity, read from system-parameters.json.</summary>
[DocumentationInfo("Seed model representing a system parameter loaded from JSON.", "Maps to SystemParameterEntity. Idempotency key: Key. ValueType maps to SystemParameterValueType enum integer.")]
public sealed class SystemParameterSeedModel
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public int ValueType { get; set; }
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsActive { get; set; } = true;
}
