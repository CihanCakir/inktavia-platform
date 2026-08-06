using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.UpdateCargoDryProduct;

[DocumentationInfo("Update CargoDry product BFF command handler",
    "Calls the CargoDry admin update-product endpoint and returns the updated product DTO.")]
public sealed class UpdateCargoDryProductBffCommandHandler
    : AizenCommandHandler<UpdateCargoDryProductBffCommand, UpdateCargoDryProductBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public UpdateCargoDryProductBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<UpdateCargoDryProductBffResponse?> Handle(
        UpdateCargoDryProductBffCommand request, CancellationToken ct)
    {
        var product = await _remote.UpdateProductAsync(
            request.ProductCode,
            new UpdateProductBffRequest
            {
                Name           = request.Name,
                Description    = request.Description,
                ValidityDays   = request.ValidityDays,
                RetailPrice    = request.RetailPrice,
                CurrencyCode   = request.CurrencyCode,
                HasSmartDevice = request.HasSmartDevice,
                DeviceType     = request.DeviceType,
                IsActive       = request.IsActive,
            }, ct);

        return new UpdateCargoDryProductBffResponse { Product = product };
    }
}
