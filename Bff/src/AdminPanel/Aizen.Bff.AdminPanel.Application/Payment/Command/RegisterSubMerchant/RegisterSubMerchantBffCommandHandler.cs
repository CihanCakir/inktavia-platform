using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.RegisterSubMerchant;

[DocumentationInfo("Register sub-merchant BFF command handler (BE-I1/#113)",
    "Forwards the admin register action ({DataSubmitted, Rejected} → SubMerchantCreated) to the Payment module. " +
    "The manual/dev gateway mints a synthetic key; iyzico uses the P9 registration. Returns the resulting onboarding " +
    "status + split-eligibility so the UI can refresh the queue row without a second fetch.")]
public sealed class RegisterSubMerchantBffCommandHandler
    : AizenCommandHandler<RegisterSubMerchantBffCommand, SubMerchantOnboardingMutateBffResponse>
{
    private readonly IPaymentRemoteCall _payment;

    public RegisterSubMerchantBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<SubMerchantOnboardingMutateBffResponse?> Handle(
        RegisterSubMerchantBffCommand request, CancellationToken ct)
    {
        var result = await _payment.RegisterSubMerchantAsync(
            request.ProviderProfileId,
            new RegisterSubMerchantBffRequest(
                request.LegalName, request.Email, request.Iban, request.SubMerchantType,
                request.TaxNumber, request.TaxOffice, request.GsmNumber,
                request.ContactName, request.ContactSurname, request.IdentityNumber),
            ct);

        return new SubMerchantOnboardingMutateBffResponse { Result = result };
    }
}
