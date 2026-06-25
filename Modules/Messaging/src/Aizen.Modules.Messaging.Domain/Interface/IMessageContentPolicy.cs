using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Domain.Interface;

[DocumentationInfo("Message content policy interface",
    "Domain contract for content moderation. Detects prohibited content, off-platform solicitation, channel abuse.")]
public interface IMessageContentPolicy
{
    /// <summary>
    /// Evaluates content against all active policy rules.
    /// Returns (isAllowed, reason). If isAllowed=false, reason explains the violation.
    /// </summary>
    Task<ContentPolicyResult> EvaluateAsync(string content, long senderUserId, MessageType messageType = MessageType.Text, CancellationToken ct = default);
}

public sealed record ContentPolicyResult(
    bool IsAllowed,
    bool RequiresReview,
    string? ViolationReason,
    string? PolicyCode
)
{
    public static ContentPolicyResult Allow() => new(true, false, null, null);
    public static ContentPolicyResult Review(string reason) => new(true, true, reason, "REVIEW_REQUIRED");
    public static ContentPolicyResult Block(string reason, string code) => new(false, false, reason, code);
}
