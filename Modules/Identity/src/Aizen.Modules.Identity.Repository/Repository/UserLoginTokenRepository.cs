using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Identity.Repository.Identity
{
    public class UserLoginTokenRepository : IUserLoginTokenRepository
    {
        private readonly IdentityDbContext _context;

        public UserLoginTokenRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserLoginTokenEntity>> GetAllTokensByUserIdAsync(long userId)
        {
            return await _context.UserLoginTokens
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        public async Task<UserLoginTokenEntity?> GetLatestValidTokenAsync(long userId)
        {
            return await _context.UserLoginTokens
                .Where(x => x.UserId == userId && !x.IsRevoked && x.AccessTokenExpiration > DateTime.UtcNow)
                .OrderByDescending(x => x.AccessTokenExpiration)
                .FirstOrDefaultAsync();
        }

        public async Task<UserLoginTokenEntity?> GetByAccessTokenAsync(string accessToken)
        {
            return await _context.UserLoginTokens
                .FirstOrDefaultAsync(x => x.AccessToken == accessToken);
        }

        public async Task<UserLoginTokenEntity?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.UserLoginTokens
                .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
        }

        public async Task AddAsync(UserLoginTokenEntity token)
        {
            await _context.UserLoginTokens.AddAsync(token);
        }

        public void Update(UserLoginTokenEntity token)
        {
            _context.UserLoginTokens.Update(token);
        }

        public async Task RevokeOldTokensAsync(long userId, int maxTokenCount = 2)
        {
            var tokens = await _context.UserLoginTokens
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.AccessTokenExpiration)
                .ToListAsync();

            var tokensToKeep = tokens.Take(maxTokenCount).ToList();
            var tokensToRevoke = tokens.Skip(maxTokenCount);

            foreach (var token in tokensToRevoke)
            {
                token.IsRevoked = true;
                _context.UserLoginTokens.Update(token);
            }
        }

        public async Task RevokeAllTokensAsync(long userId)
        {
            var tokens = await _context.UserLoginTokens
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                _context.UserLoginTokens.Update(token);
            }
        }

        public async Task<bool> ExistsByDeviceAsync(long userId, string deviceId)
        {
            return await _context.UserLoginTokens
                .AnyAsync(x => x.UserId == userId && x.DeviceId == deviceId && !x.IsRevoked);
        }
    }
}
// This code is part of the Aizen project, which is licensed under the GNU General Public License v3.0.
// You can redistribute it and/or modify it under the terms of the GPL-3.0
// For more details, see the LICENSE file in the root directory of this project.