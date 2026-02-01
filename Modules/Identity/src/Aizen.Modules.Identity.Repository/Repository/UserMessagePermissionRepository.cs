using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Repository.Identity.Repository
{
    using Aizen.Modules.Identity.Domain.Entities;
    using Aizen.Modules.Identity.Domain.Interface.Repository;
    using Aizen.Modules.Identity.Repository.Context;
    using Microsoft.EntityFrameworkCore;

    public sealed class UserMessagePermissionRepository : IUserMessagePermissionRepository
    {
        private readonly IdentityDbContext _db;

        public UserMessagePermissionRepository(IdentityDbContext db)
            => _db = db;

        public async Task AddOrUpdateAsync(UserMessagePermissionEntity entity, CancellationToken ct)
        {
            var set = _db.Set<UserMessagePermissionEntity>();

            // 1) Var mı?
            var existing = await set.FirstOrDefaultAsync(p =>
                p.UserId == entity.UserId &&
                p.PermissionType == entity.PermissionType &&
                p.PermissionContentId == entity.PermissionContentId, ct);

            if (existing is null)
            {
                // 2) Yoksa ekle (unique index yarışına karşı try/catch)
                await set.AddAsync(entity, ct);
                try
                {
                    await _db.SaveChangesAsync(ct);
                    return;
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    // Aynı anda başka thread eklediyse: yeniden yükle ve update’e düş
                    _db.Entry(entity).State = EntityState.Detached;
                }

                // 2b) Yeniden oku ve güncelle
                existing = await set.FirstOrDefaultAsync(p =>
                    p.UserId == entity.UserId &&
                    p.PermissionType == entity.PermissionType &&
                    p.PermissionContentId == entity.PermissionContentId, ct);
            }

            if (existing is not null)
            {
                // 3) Upsert (entity davranışına delegasyon)
                existing.ApplyUpsert(entity.PermissionValue, entity.ActiveProfileId);
                set.Update(existing);
                await _db.SaveChangesAsync(ct);
            }
        }

        // SQL Server: 2601/2627; PostgreSQL: 23505; MySQL: 1062
        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return msg.Contains("2601") || msg.Contains("2627") || // SQL Server
                   msg.Contains("23505") ||                        // PostgreSQL
                   msg.Contains("1062");                           // MySQL
        }
    }

}