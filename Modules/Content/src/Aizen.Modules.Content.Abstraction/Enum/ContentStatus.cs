namespace Aizen.Modules.Content.Abstraction.Enum;

/// <summary>Lifecycle status of a content item. Transitions: Draft → Scheduled → Published → Archived.</summary>
public enum ContentStatus
{
    Draft,
    Scheduled,
    Published,
    Archived
}
