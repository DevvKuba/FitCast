using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace ClientDashboard_API.Data
{
    public class EmailVerificationTokenRepository(DataContext context) : IEmailVerificationTokenRepository
    {
        public async Task<EmailVerificationToken?> GetEmailVerificationTokenByIdAsync(int tokenId)
        {
            var token = await context.EmailVerificationToken.Where(t => t.Id == tokenId).FirstOrDefaultAsync();
            return token;
        }

        public async Task<EmailVerificationToken?> GetEmailVerificationTokenByIdWithTrainerAsync(int tokenId)
        {
            var token = await context.EmailVerificationToken
                .Where(t => t.Id == tokenId)
                .Include(t => t.Trainer)
                .FirstOrDefaultAsync();
            return token;
        }

        public async Task<EmailVerificationToken?> GetEmailVerificationTokenByTokenHashAsync(string tokenHash)
        {
            var token = await context.EmailVerificationToken.Where(t => t.TokenHash == tokenHash).FirstOrDefaultAsync();
            return token;
        }

        public async Task<List<EmailVerificationToken>> GetAllExpiredOrConsumedTokensAsync()
        {
            var invalidTokens = await context.EmailVerificationToken
                .Where(t => DateTime.UtcNow >= t.ExpiresOnUtc ||
                t.IsConsumed == true)
                .ToListAsync();
            return invalidTokens;
        }

        public async Task<EmailVerificationToken?> ValidateTokenAsync(EmailVerificationToken token)
        {
            if (token == null || token.IsConsumed || DateTime.UtcNow >= token.ExpiresOnUtc) return null;

            return token;
        }

        public async Task AddEmailVerificationTokenAsync(EmailVerificationToken token)
        {
            await context.AddAsync(token);
        }

        public void ConsumeToken(EmailVerificationToken token)
        {
            token.IsConsumed = true;
            token.ConsumedAt = DateTime.UtcNow;
        }

        public void RemoveToken(EmailVerificationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
