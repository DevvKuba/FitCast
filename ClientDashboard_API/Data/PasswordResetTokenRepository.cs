using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ClientDashboard_API.Data
{
    public class PasswordResetTokenRepository(DataContext context) : IPasswordResetTokenRepository
    {
        public async Task<PasswordResetToken?> GetPasswordResetTokenByIdAsync(int tokenId)
        {
            return await context.PasswordResetToken.Where(t => t.Id == tokenId).FirstOrDefaultAsync();
        }

        public async Task<PasswordResetToken?> GetPasswordResetTokenByTokenHashAsync(string tokenHash)
        {
            return await context.PasswordResetToken.Where(t => t.TokenHash == tokenHash).FirstOrDefaultAsync();
        }

        public async Task<List<PasswordResetToken>> GetAllExpiredOrConsumedTokensAsync()
        {
            var invalidTokens = await context.PasswordResetToken
               .Where(t => DateTime.UtcNow >= t.ExpiresOnUtc ||
               t.IsConsumed == true)
               .ToListAsync();
            return invalidTokens;
        }

        public async Task<PasswordResetToken?> ValidateTokenAsync(PasswordResetToken token)
        {
            if (token == null || token.IsConsumed || DateTime.UtcNow >= token.ExpiresOnUtc) return null;

            return token;
        }

        public async Task AddPasswordResetTokenAsync(PasswordResetToken token)
        {
            await context.AddAsync(token);
        }

        public void ConsumeToken(PasswordResetToken token)
        {
            token.IsConsumed = true;
            token.ConsumedAt = DateTime.UtcNow;
        }

        public void RemoveToken(PasswordResetToken token)
        {
            context.PasswordResetToken.Remove(token);
        }
    }
}
