using Aizen.Modules.Identity.Domain.Entities.UserAgreement;

namespace Aizen.Modules.Identity.Domain.Interface.Repository
{
    public interface IAgreementRepository
    {
        /// <summary>
        /// Belirtilen türlerdeki sözleşmelerin en son versiyonlarını kontrol eder.
        /// </summary>
        Task<List<AgreementEntity>> GetUnapprovedAgreementsAsync(long userId, params string[] agreementTypes);

        /// <summary>
        /// Belirli bir sözleşmenin kullanıcının onaylayıp onaylamadığını kontrol eder.
        /// </summary>
        Task<bool> HasUserApprovedAgreementAsync(long userId, long agreementId);

        /// <summary>
        /// Kullanıcının tüm güncel sözleşmeleri onaylayıp onaylamadığını kontrol eder.
        /// </summary>
        Task<bool> HasUserApprovedAllAsync(long userId, params string[] agreementTypes);

        /// <summary>
        /// Kullanıcının belirli bir sözleşmeyi onaylamasını sağlar.
        /// </summary>
        Task<UserAgreementEntity> ApproveAgreementAsync(long userId, AgreementEntity agreement);


        Task<List<AgreementEntity>> CheckAgreementsAsync(params string[] agrementTypes);
        Task<int> CheckAgreement(int id, int userId = 0);
        
        /// <summary>
        /// Belirtilen kullanıcı ilgili agreement'ın en güncel versiyonunu onaylamış mı?
        /// </summary>
        Task<int> HasUserApprovedAgreementAsync(int agreementId, long userId);
    }
}