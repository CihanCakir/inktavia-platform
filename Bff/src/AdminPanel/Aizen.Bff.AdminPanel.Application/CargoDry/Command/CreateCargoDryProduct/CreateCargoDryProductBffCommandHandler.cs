using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.CreateCargoDryProduct;

[DocumentationInfo("Create CargoDry product BFF command handler",
    "Calls the CargoDry admin create-product endpoint and returns the newly created product.")]
public sealed class CreateCargoDryProductBffCommandHandler
    : AizenCommandHandler<CreateCargoDryProductBffCommand, CreateCargoDryProductBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public CreateCargoDryProductBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<CreateCargoDryProductBffResponse?> Handle(
        CreateCargoDryProductBffCommand request, CancellationToken ct)
    {
        var product = await _remote.CreateProductAsync(
            new CreateProductBffRequest
            {
                ProductCode    = request.ProductCode,
                Name           = request.Name,
                Description    = request.Description,
                ValidityDays   = request.ValidityDays,
                RetailPrice    = request.RetailPrice,
                CurrencyCode   = request.CurrencyCode,
                HasSmartDevice = request.HasSmartDevice,
                DeviceType     = request.DeviceType,
            }, ct);

        return new CreateCargoDryProductBffResponse { Product = product };
    }
}
