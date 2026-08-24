using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface IEmailLayoutRepository
{
    /// <summary>Aktif (IsActive) layout'u koda göre döner; yoksa null.</summary>
    Task<EmailLayoutEntity?> GetActiveByCodeAsync(string code, CancellationToken ct = default);

    Task AddAsync(EmailLayoutEntity entity, CancellationToken ct = default);
}
