namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// Request sent from ServiceRequest module → Payment module to release the held escrow
/// after admin approves SR completion.
/// </summary>
public sealed class ReleaseEscrowRemoteCallRequest
{
    /// <summary>Admin user ID authorising the release.</summary>
    public required long ApprovedByUserId { get; init; }

    /// <summary>Optional admin note attached to the payout record.</summary>
    public string? AdminNote { get; init; }
}
