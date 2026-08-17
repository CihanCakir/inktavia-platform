namespace Aizen.Modules.Identity.Domain.Interface.Service
{
    /// <summary>
    /// Synchronizes provider Keycloak realm roles with Identity approval/suspend transitions.
    /// Coarse authorization helper only — runtime Identity ApprovalStatus/ProfileStatus remains authoritative.
    /// No-ops when the user has no linked Keycloak subject or when sync is disabled.
    /// </summary>
    public interface IProviderKeycloakRoleSyncService
    {
        /// <summary>Approved: remove provider_pending, add provider_user, remove provider_restricted.</summary>
        Task OnApprovedAsync(string? keycloakSubjectId, CancellationToken cancellationToken = default);

        /// <summary>Suspended: remove provider_user, add provider_restricted, optionally revoke sessions.</summary>
        Task OnSuspendedAsync(string? keycloakSubjectId, CancellationToken cancellationToken = default);

        /// <summary>Reactivated (Approved + Active): add provider_user, remove provider_restricted.</summary>
        Task OnReactivatedAsync(string? keycloakSubjectId, CancellationToken cancellationToken = default);
    }
}
