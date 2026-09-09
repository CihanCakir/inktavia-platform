using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.RegisterSubMerchant;

public sealed class RegisterSubMerchantBffCommand : AizenCommand<SubMerchantOnboardingMutateBffResponse>
{
    public long    ProviderProfileId { get; init; }
    public string? LegalName         { get; init; }
    public string? Email             { get; init; }
    public string? Iban              { get; init; }
    public string? SubMerchantType   { get; init; }
    public string? TaxNumber         { get; init; }
    public string? TaxOffice         { get; init; }
    public string? GsmNumber         { get; init; }
    public string? ContactName       { get; init; }
    public string? ContactSurname    { get; init; }
    public string? IdentityNumber    { get; init; }
}
