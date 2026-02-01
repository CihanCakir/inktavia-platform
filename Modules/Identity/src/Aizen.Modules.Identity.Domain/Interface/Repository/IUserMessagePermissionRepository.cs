using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Interface.Repository
{
    public interface IUserMessagePermissionRepository
    {
        /// <summary>
        /// (UserId, PermissionType, PermissionContentId) anahtarına göre upsert.
        /// Mevcutsa değerleri günceller; yoksa ekler.
        /// </summary>
        Task AddOrUpdateAsync(UserMessagePermissionEntity entity, CancellationToken ct);
    }
}