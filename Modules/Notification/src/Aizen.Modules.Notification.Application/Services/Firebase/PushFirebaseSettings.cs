using System.Text.Json.Serialization;

namespace Aizen.Modules.Notification.Application.Services.Firebase;

/// <summary>
/// BE-MO9a — the Firebase service-account fields, bound from the appsettings <c>PushNotification:Firebase</c> section.
/// Committed config holds placeholders only; the real values (especially <see cref="PrivateKey"/>) are injected via
/// env / k8s secret — the same posture as the VAPID keys. When not configured, the module keeps the dev
/// <c>FcmSenderStub</c> so it builds/runs/tests with no credentials.
/// </summary>
public sealed class PushFirebaseSettings
{
    public const string SectionName = "PushNotification:Firebase";

    public string? Type { get; set; }
    public string? ProjectId { get; set; }
    public string? PrivateKeyId { get; set; }
    /// <summary>The service-account private key — NEVER committed; injected via env/secret.</summary>
    public string? PrivateKey { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientId { get; set; }
    public string? AuthUri { get; set; }
    public string? TokenUri { get; set; }
    public string? AuthProviderX509CertUrl { get; set; }
    public string? ClientX509CertUrl { get; set; }

    /// <summary>
    /// True only when the minimum credentials needed to authenticate a service account are present. Drives the
    /// dev-safe DI switch (real <c>FcmSender</c> when configured, else the stub). "ProjectId present" alone is not
    /// enough — the private key + client email are required to actually mint a Google credential.
    /// </summary>
    [JsonIgnore]
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ProjectId)
        && !string.IsNullOrWhiteSpace(PrivateKey)
        && !string.IsNullOrWhiteSpace(ClientEmail);

    /// <summary>Serializes these fields into the snake_case Google service-account JSON that
    /// <c>GoogleCredential.FromJson</c> consumes.</summary>
    public string ToServiceAccountJson()
        => System.Text.Json.JsonSerializer.Serialize(new ServiceAccountModel
        {
            Type                    = Type ?? "service_account",
            ProjectId               = ProjectId,
            PrivateKeyId            = PrivateKeyId,
            PrivateKey              = PrivateKey,
            ClientEmail             = ClientEmail,
            ClientId                = ClientId,
            AuthUri                 = AuthUri ?? "https://accounts.google.com/o/oauth2/auth",
            TokenUri                = TokenUri ?? "https://oauth2.googleapis.com/token",
            AuthProviderX509CertUrl = AuthProviderX509CertUrl ?? "https://www.googleapis.com/oauth2/v1/certs",
            ClientX509CertUrl       = ClientX509CertUrl,
        });

    /// <summary>The snake_case DTO bridging the flat settings → the Google credential JSON (mirrors Metropol's
    /// ServiceAccountModel).</summary>
    private sealed class ServiceAccountModel
    {
        [JsonPropertyName("type")]                        public string? Type { get; set; }
        [JsonPropertyName("project_id")]                  public string? ProjectId { get; set; }
        [JsonPropertyName("private_key_id")]              public string? PrivateKeyId { get; set; }
        [JsonPropertyName("private_key")]                 public string? PrivateKey { get; set; }
        [JsonPropertyName("client_email")]                public string? ClientEmail { get; set; }
        [JsonPropertyName("client_id")]                   public string? ClientId { get; set; }
        [JsonPropertyName("auth_uri")]                    public string? AuthUri { get; set; }
        [JsonPropertyName("token_uri")]                   public string? TokenUri { get; set; }
        [JsonPropertyName("auth_provider_x509_cert_url")] public string? AuthProviderX509CertUrl { get; set; }
        [JsonPropertyName("client_x509_cert_url")]        public string? ClientX509CertUrl { get; set; }
    }
}
