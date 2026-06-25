namespace Aizen.Bff.AdminPanel.Application.Common.Services;

[DocumentationInfo("BFF Keycloak service token provider", "Provides a cached Keycloak service token for the admin-panel-bff client credentials flow.")]
public interface IAdminPanelBffKeycloakServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
