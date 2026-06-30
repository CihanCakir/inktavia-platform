using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface IProviderPaymentProfileRepository
{
    Task<ProviderPaymentProfileEntity?> GetByProviderProfileIdAsync(long providerProfileId, CancellationToken ct = default);
    Task<bool> ExistsAsync(long providerProfileId, CancellationToken ct = default);

    Task AddAsync(ProviderPaymentProfileEntity entity, CancellationToken ct = default);
    void Update(ProviderPaymentProfileEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
