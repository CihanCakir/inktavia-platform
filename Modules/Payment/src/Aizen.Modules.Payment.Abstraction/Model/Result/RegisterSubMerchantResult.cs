namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record RegisterSubMerchantResult(
    long   ProviderProfileId,
    string SubMerchantKey,
    string GatewayProvider
);
