using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Enforces the content lifecycle (§9 C4). Valid paths:
///   Draft → Scheduled → Published → Archived,
///   plus Publish direct from Draft, Unpublish (Published → Draft) and Archive from any live state.
/// Editing (update/translation/placement/audience/media) is allowed on any non-Archived item.
/// Invalid transitions throw <see cref="AizenBusinessException"/>.
/// </summary>
public static class ContentStatusTransition
{
    public static void EnsureCanSchedule(ContentStatus current)
    {
        if (current is not (ContentStatus.Draft or ContentStatus.Scheduled))
            throw new AizenBusinessException(
                $"Cannot schedule content in status '{current}'. Only Draft or Scheduled items can be (re)scheduled.");
    }

    public static void EnsureCanPublish(ContentStatus current)
    {
        if (current is not (ContentStatus.Draft or ContentStatus.Scheduled))
            throw new AizenBusinessException(
                $"Cannot publish content in status '{current}'. Only Draft or Scheduled items can be published.");
    }

    public static void EnsureCanUnpublish(ContentStatus current)
    {
        if (current is not ContentStatus.Published)
            throw new AizenBusinessException(
                $"Cannot unpublish content in status '{current}'. Only Published items can be unpublished.");
    }

    public static void EnsureCanArchive(ContentStatus current)
    {
        if (current is ContentStatus.Archived)
            throw new AizenBusinessException("Content is already archived.");
    }

    public static void EnsureEditable(ContentStatus current)
    {
        if (current is ContentStatus.Archived)
            throw new AizenBusinessException("Archived content cannot be edited. Restore it first (future capability).");
    }
}
