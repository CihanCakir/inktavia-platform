namespace Aizen.Modules.Payment.Abstraction.Requests;

/// <summary>
/// Admin API request: releases escrow funds to the provider after SR completion.
/// Creates a PayoutRecord for the provider.
/// </summary>
public sealed class ReleaseEscrowRequest
{
    public required long    ApprovedByUserId { get; init; }
    public          string? AdminNote        { get; init; }
}
