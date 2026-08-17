namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Extended kit DTO for admin single-kit detail and admin lookup responses.
/// Superset of <see cref="CargoDryKitDto"/> — adds audit-sensitive and
/// admin-only fields that are not exposed in the paginated list endpoint.
/// Phase 8B (July 2026).
/// </summary>
public sealed class CargoDryKitDetailDto : CargoDryKitDto
{
    /// <summary>Consignment agreement this kit is linked to. Null = no consignment.</summary>
    public long? ConsignmentAgreementId { get; init; }

    /// <summary>
    /// Raw QR code payload (static content, not an activation secret).
    /// The short-lived JWT activationToken is a separate field returned only by ValidateKit.
    /// </summary>
    public string? QrPayload { get; init; }

    /// <summary>Admin-supplied revocation reason. Non-null only when Status = Revoked.</summary>
    public string? RevokeReason { get; init; }

    /// <summary>UTC timestamp when the kit was revoked. Null unless Status = Revoked.</summary>
    public DateTimeOffset? RevokedAt { get; init; }

    /// <summary>Entity creation timestamp (UTC).</summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>Last entity update timestamp (UTC). Null if never modified after creation.</summary>
    public DateTime? UpdatedAtUtc { get; init; }
}
