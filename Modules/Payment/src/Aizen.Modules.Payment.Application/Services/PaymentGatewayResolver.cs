using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Resolves the active IPaymentGatewayProvider based on the PAYMENT_GATEWAY_ACTIVE
/// system parameter (or environment variable for MVP). Switching gateways requires
/// only a config change — no code change.
/// </summary>
public sealed class PaymentGatewayResolver
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<PaymentGatewayResolver> _logger;

    public PaymentGatewayResolver(IServiceProvider provider, ILogger<PaymentGatewayResolver> logger)
    {
        _provider = provider;
        _logger   = logger;
    }

    public IPaymentGatewayProvider Resolve()
    {
        // MVP: read from env var. Post-MVP: read from SystemParameter entity via ReferenceData.
        var activeKey = Environment.GetEnvironmentVariable("PAYMENT_GATEWAY_ACTIVE")
            ?? "manual";

        var gateway = _provider.GetKeyedService<IPaymentGatewayProvider>(activeKey);
        if (gateway is null)
            throw new AizenBusinessException((int)PaymentErrorCode.GatewayProviderNotFound);

        _logger.LogDebug("Resolved payment gateway: {Key}", activeKey);
        return gateway;
    }
}
