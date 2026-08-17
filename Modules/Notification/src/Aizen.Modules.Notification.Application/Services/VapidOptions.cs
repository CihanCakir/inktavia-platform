namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// VAPID configuration for Web Push. The private key MUST come from a secret store,
/// never from appsettings.json.
///
/// Generate a key pair with:
///   dotnet tool install --global dotnet-webpush
///   dotnet webpush generate-vapid-keys
///
/// Or in Node: npx web-push generate-vapid-keys
///
/// The public key is shared with the frontend (applicationServerKey in pushManager.subscribe).
/// </summary>
public sealed class VapidOptions
{
    public const string SectionName = "Vapid";

    public string Subject { get; set; } = default!;
    public string PublicKey { get; set; } = default!;
    public string PrivateKey { get; set; } = default!;
}
