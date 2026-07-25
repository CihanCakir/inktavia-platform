using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Request;

namespace Aizen.Bff.MarineProvider.Application.Payment;

public sealed class UpsertProviderPaymentProfileBffCommandHandler
    : AizenCommandHandler<UpsertProviderPaymentProfileBffCommand, ProviderPaymentProfileDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly IPaymentRemoteCall _c;

    public UpsertProviderPaymentProfileBffCommandHandler(IProviderProfileResolver r, IProviderIdentityHolder h, IPaymentRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<ProviderPaymentProfileDto?> Handle(UpsertProviderPaymentProfileBffCommand cmd, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _c.UpsertPaymentProfile(new UpsertProviderPaymentProfileRequest
        {
            Iban      = cmd.Iban,
            LegalName = cmd.LegalName,
            TaxNumber = cmd.TaxNumber,
        })).Body;
    }
}
