using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;

namespace Aizen.Modules.Payment.Abstraction.Requests;

/// <summary>
/// Admin API request: creates a new payment escrow transaction.
/// Idempotent via IdempotencyKey — duplicate requests return the existing record.
/// </summary>
public sealed class CreateEscrowRequest
{
    public required string             IdempotencyKey     { get; init; }
    public required TransactionContext Context            { get; init; }
    public required TransactionType    TransactionType    { get; init; }
    public required long               PayerProfileId     { get; init; }
    public required long               RecipientProfileId { get; init; }
    public required decimal            GrossAmount        { get; init; }
    public          decimal            DiscountAmount     { get; init; }
    public required string             CurrencyCode       { get; init; }
    public          long?              ProviderPlanId     { get; init; }
    public          string?            CategoryCode       { get; init; }
    public          bool               EscrowRequired     { get; init; } = true;
}
