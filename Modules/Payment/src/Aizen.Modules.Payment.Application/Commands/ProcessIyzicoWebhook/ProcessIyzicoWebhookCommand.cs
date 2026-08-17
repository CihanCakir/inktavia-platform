using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.ProcessIyzicoWebhook;

public sealed class ProcessIyzicoWebhookCommand : AizenCommand<ProcessIyzicoWebhookResult>
{
    /// <summary>The "token" field from Iyzico's form-POST body.</summary>
    public string? Token { get; init; }

    /// <summary>The "status" field from Iyzico's form-POST body.</summary>
    public string? Status { get; init; }

    /// <summary>HMAC signature from the X-IYZ-SIGNATURE-V3 header (may be null).</summary>
    public string? Signature { get; init; }

    // ── BE-P9-fix §2: HPP webhook V3 payload fields ──
    public string? IyziEventType         { get; init; }   // e.g. CHECKOUT_FORM_AUTH
    public string? IyziPaymentId         { get; init; }
    public string? PaymentConversationId { get; init; }
    public string? IyziReferenceCode     { get; init; }   // idempotency key

    /// <summary>All incoming HTTP headers — forwarded for gateway-level inspection.</summary>
    public Dictionary<string, string> Headers { get; init; } = new();
}
