namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Result of an admin kit lookup by kit code, serial number, or numeric id.
/// Phase 8B (July 2026).
/// </summary>
public sealed class CargoDryKitLookupResultDto
{
    /// <summary>True when a kit was found matching the query.</summary>
    public bool Found { get; init; }

    /// <summary>
    /// How the kit was matched. One of: KitCode | SerialNumber | Id | NotFound.
    /// </summary>
    public string MatchType { get; init; } = "NotFound";

    /// <summary>Full kit detail when Found = true. Null when Found = false.</summary>
    public CargoDryKitDetailDto? Kit { get; init; }

    /// <summary>
    /// Non-blocking warnings, e.g. "Kit expires in 3 days", "Kit is in Revoked status".
    /// Empty list when no warnings apply.
    /// </summary>
    public List<string> Warnings { get; init; } = [];
}
