using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("Orphan file cleanup requested message", "Fire-and-forget message that triggers a batch cleanup of files with no active owner references older than the configured TTL.")]
public sealed class OrphanFileCleanupRequestedMessage : AizenBaseMessage
{
    /// <summary>
    /// Number of hours a file must be unclaimed before it is eligible for cleanup. Default: 24.
    /// </summary>
    public int CleanupTtlHours { get; set; } = 24;
}
