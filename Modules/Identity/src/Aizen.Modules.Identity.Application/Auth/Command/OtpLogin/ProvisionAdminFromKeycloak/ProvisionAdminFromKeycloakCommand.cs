using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ProvisionAdminFromKeycloak;

public sealed class ProvisionAdminFromKeycloakCommand : AizenCommand<AdminProvisionResult>
{
    public string KeycloakSubjectId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? EmailVerified { get; set; }
}
