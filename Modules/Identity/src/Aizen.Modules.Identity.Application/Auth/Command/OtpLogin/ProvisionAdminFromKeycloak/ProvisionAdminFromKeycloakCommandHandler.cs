using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ProvisionAdminFromKeycloak;

public sealed class ProvisionAdminFromKeycloakCommandHandler
    : AizenCommandHandler<ProvisionAdminFromKeycloakCommand, AdminProvisionResult>
{
    private readonly IAdminKeycloakProvisioningDomainService _service;

    public ProvisionAdminFromKeycloakCommandHandler(IAdminKeycloakProvisioningDomainService service)
    {
        _service = service;
    }

    public override async Task<AdminProvisionResult?> Handle(
        ProvisionAdminFromKeycloakCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ProvisionAsync(new ProvisionAdminFromKeycloakDomainModel
        {
            KeycloakSubjectId = request.KeycloakSubjectId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailVerified = request.EmailVerified
        }, cancellationToken);

        return new AdminProvisionResult
        {
            Provisioned = true,
            UserId = result.User.Id,
            AdminProfileId = result.Profile?.Id,
            CreatedUser = result.CreatedUser,
            CreatedProfile = result.CreatedProfile,
            AlreadyLinked = result.AlreadyLinked,
            Warnings = result.Warnings
        };
    }
}
