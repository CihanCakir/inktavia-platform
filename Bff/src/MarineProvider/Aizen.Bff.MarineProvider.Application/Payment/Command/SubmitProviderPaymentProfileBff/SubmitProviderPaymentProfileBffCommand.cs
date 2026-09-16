using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Request;

namespace Aizen.Bff.MarineProvider.Application.Payment;

/// <summary>Provider submits full KYC → Payment persists DataSubmitted + enqueues async provisioning.</summary>
public sealed class SubmitProviderPaymentProfileBffCommand : AizenCommand<ProviderPaymentProfileDto>
{
    public string  Iban            { get; init; } = default!;
    public string? LegalName       { get; init; }
    public string? TaxNumber       { get; init; }
    public string? SubMerchantType { get; init; }
    public string? Email           { get; init; }
    public string? TaxOffice       { get; init; }
    public string? GsmNumber       { get; init; }
    public string? ContactName     { get; init; }
    public string? ContactSurname  { get; init; }
    public string? IdentityNumber  { get; init; }
}

public sealed class SubmitProviderPaymentProfileBffCommandHandler
    : AizenCommandHandler<SubmitProviderPaymentProfileBffCommand, ProviderPaymentProfileDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly IPaymentRemoteCall _c;

    public SubmitProviderPaymentProfileBffCommandHandler(IProviderProfileResolver r, IProviderIdentityHolder h, IPaymentRemoteCall c)
    { _resolver = r; _h = h; _c = c; }

    public override async Task<ProviderPaymentProfileDto?> Handle(SubmitProviderPaymentProfileBffCommand cmd, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _c.SubmitPaymentProfile(new SubmitProviderPaymentProfileRequest
        {
            Iban            = cmd.Iban,
            LegalName       = cmd.LegalName,
            TaxNumber       = cmd.TaxNumber,
            SubMerchantType = cmd.SubMerchantType,
            Email           = cmd.Email,
            TaxOffice       = cmd.TaxOffice,
            GsmNumber       = cmd.GsmNumber,
            ContactName     = cmd.ContactName,
            ContactSurname  = cmd.ContactSurname,
            IdentityNumber  = cmd.IdentityNumber,
        })).Body;
    }
}
