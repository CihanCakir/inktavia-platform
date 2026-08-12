namespace Aizen.Modules.Messaging.Abstraction.Response.Messaging;

/// <summary>
/// BE_WC4a — result of the idempotent ensure-by-context (get-or-create) of a ServiceRequest conversation. Returns the
/// numeric conversation id the caller (a BFF send handler) passes straight into <c>SendMessage</c>. <see cref="Created"/>
/// is true only when this call created the conversation (false when it already existed). <see cref="ConversationId"/> is
/// 0 when the context could not be resolved (e.g. the SR does not exist) — the BFF treats that as "fall back to SR".
/// </summary>
public sealed class EnsureConversationByContextResponse
{
    public long ConversationId { get; init; }
    public bool Created { get; init; }
}
