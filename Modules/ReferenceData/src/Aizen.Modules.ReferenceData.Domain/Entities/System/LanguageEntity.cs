using Aizen.Core.Domain;

namespace Aizen.Modules.ReferenceData.Domain.Entities.System;

public sealed class LanguageEntity : AizenEntityWithAudit
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string NativeName { get; private set; } = default!;
    public bool IsDefault { get; private set; }
    public LanguageEntity() { }

    public static LanguageEntity Create(string code, string name, string nativeName, bool isDefault)
    {
        return new LanguageEntity
        {
            Code = code.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            NativeName = nativeName.Trim(),
            IsDefault = isDefault,
            IsActive = true
        };
    }
}
