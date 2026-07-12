using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities.UserAgreement
{
public class UserAgreementEntity : AizenEntityWithAudit
{
    public long UserId { get; private set; }
    public virtual UserEntity User { get; private set; } = null!;

    public long AgreementId { get; private set; }
    public virtual AgreementEntity Agreement { get; private set; } = null!;

    public decimal VersionNumber { get; private set; }
    public DateTime ApprovedAt { get; private set; }

    // 📌 Domain Constructor
    // EF Core lazy-loading proxies (Castle DynamicProxy) subclass the entity, so the parameterless ctor must be
    // at least protected. A private one makes every query that materializes this type fail at runtime.
    protected UserAgreementEntity() { }

    private UserAgreementEntity(long userId, long agreementId, decimal versionNumber)
    {
        UserId = userId;
        AgreementId = agreementId;
        VersionNumber = versionNumber;
        ApprovedAt = DateTime.UtcNow;
        CreateDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Kullanıcının bir sözleşmeyi onaylamasını temsil eden fabrika metodu.
    /// </summary>
    public static UserAgreementEntity ApproveAgreement(long userId, AgreementEntity agreement)
    {
        return new UserAgreementEntity(userId, agreement.Id, agreement.LastVersionNumber);
    }

    /// <summary>
    /// Aynı sözleşmenin aynı versiyonu daha önce onaylanmış mı kontrol edilebilir.
    /// </summary>
    public bool IsSameVersion(AgreementEntity agreement)
    {
        return AgreementId == agreement.Id && VersionNumber == agreement.LastVersionNumber;
    }
}

}