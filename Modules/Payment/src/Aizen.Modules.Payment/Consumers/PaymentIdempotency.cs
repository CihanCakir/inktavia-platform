using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Consumers;

/// <summary>
/// WS1 financial defense-in-depth helper.
///
/// The financial consumers persist a natural-key marker (invoice / payout / refund / renewal charge)
/// that is protected by a partial-unique DB index. Under the two-phase double-commit (§ spike report),
/// two commit copies of the same event can race to insert the same marker. The app-level guard
/// (check-the-natural-key-first) closes the common case; this helper classifies the residual
/// insert-race loser's PostgreSQL unique-violation (SQLSTATE 23505) so the consumer can swallow it as a
/// benign "already processed" instead of throwing — leaving exactly one row and one gateway call.
/// </summary>
internal static class PaymentIdempotency
{
    /// <summary>
    /// True when the <see cref="DbUpdateException"/> wraps a PostgreSQL unique-constraint violation
    /// (SQLSTATE 23505). Mirrors the detection used by Identity's UserMessagePermissionRepository.
    /// </summary>
    public static bool IsUniqueViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("23505");   // PostgreSQL unique_violation
    }
}
