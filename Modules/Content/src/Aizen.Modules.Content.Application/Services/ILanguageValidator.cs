namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Validates translation language codes (B4). Content must not maintain its own language master list,
/// and ReferenceData.Abstraction currently exposes NO Language reference to validate against, so this
/// falls back to a deployment-configured supported set (Content:Languages:Supported; default tr,en).
/// Repoint at ReferenceData once it publishes a Language contract.
/// </summary>
public interface ILanguageValidator
{
    /// <summary>Throws AizenBusinessException when the code is not a supported language.</summary>
    void EnsureSupported(string languageCode);
}
