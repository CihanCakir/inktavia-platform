using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

/// <summary>
/// Published by CargoDry when an admin explicitly dispatches a renewal notification.
/// Consumed by the Notification module — it renders the template and delivers via
/// the specified channels (Email, Sms, InApp, Push).
/// CargoDry does NOT call any SMS/Mail provider directly.
/// Idempotency: use IdempotencyKey (RenewalCode + Channel hash) to prevent duplicate delivery.
/// Phase 11 (July 2026).
/// </summary>
public sealed class CargoDryRenewalNotificationRequestedMessage : AizenBaseMessage
{
    /// <summary>Unique correlation id for tracking delivery across Notification module logs.</summary>
    public string          CorrelationId          { get; set; } = default!;

    public long            RenewalPreparationId   { get; set; }
    public string          RenewalCode            { get; set; } = default!;
    public long            KitId                  { get; set; }
    public string          KitCode                { get; set; } = default!;
    public string          ProductCode            { get; set; } = default!;
    public string?         ProductName            { get; set; }
    public long?           OwnerUserId            { get; set; }
    public long?           VesselId               { get; set; }
    public long?           ProviderProfileId      { get; set; }

    /// <summary>Comma-separated channel names: "Email,Sms,InApp"</summary>
    public string          Channels               { get; set; } = default!;

    public string          LanguageCode           { get; set; } = "tr";
    public string          TemplateCode           { get; set; } = default!;

    public string?         RecipientEmail         { get; set; }
    public string?         RecipientPhone         { get; set; }

    public DateTimeOffset? ExpiresAtUtc           { get; set; }
    public int             DaysUntilExpiry        { get; set; }
    public decimal?        RenewalPrice           { get; set; }
    public string?         CurrencyCode           { get; set; }

    /// <summary>Per-channel idempotency key. Pass to Notification module if supported.</summary>
    public string?         IdempotencyKey         { get; set; }
    public long            RequestedByUserId      { get; set; }
    public DateTimeOffset  RequestedAtUtc         { get; set; }
}
