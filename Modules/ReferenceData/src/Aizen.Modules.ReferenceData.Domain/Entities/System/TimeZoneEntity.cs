using Aizen.Core.Domain;

namespace Aizen.Modules.ReferenceData.Domain.Entities.System;

public sealed class TimeZoneEntity : AizenEntityWithAudit
{
    public string Code { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string UtcOffset { get; private set; } = default!;

    public TimeZoneEntity() { }

    public static TimeZoneEntity Create(string code, string displayName, string utcOffset)
    {
        return new TimeZoneEntity
        {
            Code = code.Trim(),
            DisplayName = displayName.Trim(),
            UtcOffset = utcOffset.Trim(),
            IsActive = true
        };
    }
}
