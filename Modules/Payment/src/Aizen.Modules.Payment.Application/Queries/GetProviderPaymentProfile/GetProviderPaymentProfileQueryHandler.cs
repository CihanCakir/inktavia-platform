using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPaymentProfile;

public sealed class GetProviderPaymentProfileQueryHandler
    : AizenQueryHandler<GetProviderPaymentProfileQuery, ProviderPaymentProfileDto>
{
    private readonly IProviderPaymentProfileRepository _repo;

    public GetProviderPaymentProfileQueryHandler(IProviderPaymentProfileRepository repo) => _repo = repo;

    public override async Task<ProviderPaymentProfileDto?> Handle(
        GetProviderPaymentProfileQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByProviderProfileIdAsync(request.ProviderProfileId, ct);
        if (entity is null) return null;

        return new ProviderPaymentProfileDto
        {
            GatewayProvider = entity.GatewayProvider,
            HasIban         = !string.IsNullOrEmpty(entity.IbanEncrypted),
            IbanMasked      = MaskIban(entity.IbanLast4),
            LegalName       = entity.LegalName,
            TaxNumberMasked = MaskTaxNumber(entity.TaxNumber),
            Status          = entity.Status,
            VerifiedAt      = entity.VerifiedAt,
        };
    }

    private static string? MaskIban(string? last4)
        => string.IsNullOrEmpty(last4) ? null : $"TR** **** **** {last4}";

    private static string? MaskTaxNumber(string? taxNumber)
    {
        if (string.IsNullOrEmpty(taxNumber)) return null;
        if (taxNumber.Length <= 3) return new string('*', taxNumber.Length);
        return new string('*', taxNumber.Length - 3) + taxNumber[^3..];
    }
}
