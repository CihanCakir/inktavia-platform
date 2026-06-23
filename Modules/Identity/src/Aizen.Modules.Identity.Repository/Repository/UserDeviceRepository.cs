using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Identity.Repository.Identity
{
    public class UserDeviceRepository : IUserDeviceRepository
    {
        private readonly IdentityDbContext _context;

        public UserDeviceRepository(IdentityDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // 1. Cihaz bugün engellenmiş mı?
        public async Task<bool> IsDeviceBlockedTodayAsync(string deviceId, DateTime date)
        {
            return await _context.UserDeviceBlocks
                .AnyAsync(x => x.DeviceId == deviceId && x.IsActive && x.CreateDate.HasValue && x.CreateDate.Value.Date == date.Date);
        }

        // 2. Bu cihazdan bugün kaç farklı kullanıcı giriş yaptı?
        public async Task<int> GetTodayLoginUserCountByDeviceIdAsync(string deviceId, DateTime date)
        {
            return await _context.UserLoginTokens
                .Where(x => x.DeviceId == deviceId && x.CreateDate.HasValue && x.CreateDate.Value.Date == date.Date)
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync();
        }

        // 3. Cihazı engelle
        public async Task AddDeviceBlockAsync(UserDeviceBlockEntity entity)
        {
            await _context.UserDeviceBlocks.AddAsync(entity);
            await _context.SaveChangesAsync();
        }


        /// <summary>
        /// Kullanıcının cihaz kaydını ekler ya da günceller.
        /// </summary>
        public async Task AddOrUpdateDeviceAsync(long userId, string deviceId, string? notificationToken, ConsumerDeviceType deviceType, WorkshopRoleContext roleContext, long? activeProfileId)
        {
            var existingDevice = await _context.UserDevices.FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId);

            if (existingDevice == null)
            {
                var newDevice = UserDeviceEntity.Create(userId, deviceId, deviceType, notificationToken, roleContext, activeProfileId);
                await _context.UserDevices.AddAsync(newDevice);
            }
            else
            {
                existingDevice.UpdateLoginInfo(notificationToken, deviceType, roleContext, activeProfileId);
                _context.UserDevices.Update(existingDevice);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Kullanıcının aktif tüm cihazlarını getirir.
        /// </summary>
        public async Task<List<UserDeviceEntity>> GetActiveDevicesByUserIdAsync(long userId)
        {
            return await _context.UserDevices
                .Where(x => x.UserId == userId && x.IsActive)
                .ToListAsync();
        }



        /// <summary>
        /// Belirtilen kullanıcı ve cihaz ID'si ile eşleşen cihaz var mı kontrol eder.
        /// </summary>
        public async Task<bool> IsDeviceExistsAsync(long userId, string deviceId)
        {
            return await _context.UserDevices.AnyAsync(x => x.UserId == userId && x.DeviceId == deviceId);
        }

        /// <summary>
        /// Belirtilen günde (UTC) en az bir kez giriş yapmış benzersiz kullanıcı sayısını döner.
        /// </summary>
        public async Task<int> GetActiveTodayUserCountAsync(DateTime utcDate)
        {
            return await _context.UserDevices
                .Where(d => d.IsActive && d.LastLoginDate.HasValue && d.LastLoginDate.Value.Date == utcDate.Date)
                .Select(d => d.UserId)
                .Distinct()
                .CountAsync();
        }

        /// <summary>
        /// Kullanıcının tüm cihazlarını pasif hale getirir.
        /// </summary>
        public async Task RevokeAllDevicesAsync(long userId)
        {
            var devices = await _context.UserDevices
                .Where(x => x.UserId == userId && x.IsActive)
                .ToListAsync();

            foreach (var device in devices)
            {
                device.Revoke();
                _context.UserDevices.Update(device);
            }

            await _context.SaveChangesAsync();
        }
    }
}
