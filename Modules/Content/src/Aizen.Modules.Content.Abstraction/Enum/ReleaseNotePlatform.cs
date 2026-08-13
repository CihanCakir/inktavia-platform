namespace Aizen.Modules.Content.Abstraction.Enum;

/// <summary>Target platform of a release note (§3). Force-update gating is app-config, not Content.</summary>
public enum ReleaseNotePlatform
{
    iOS,
    Android,
    Web,
    All
}
