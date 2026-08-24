using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class EmailLayoutRepository : IEmailLayoutRepository
{
    private readonly NotificationDbContext _db;

    public EmailLayoutRepository(NotificationDbContext db) => _db = db;

    public Task<EmailLayoutEntity?> GetActiveByCodeAsync(string code, CancellationToken ct)
        => _db.EmailLayouts.FirstOrDefaultAsync(x => x.Code == code.ToUpperInvariant() && x.IsActive, ct);

    public async Task AddAsync(EmailLayoutEntity entity, CancellationToken ct)
    {
        await _db.EmailLayouts.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }
}
