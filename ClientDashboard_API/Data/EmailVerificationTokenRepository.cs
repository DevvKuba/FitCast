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

        public async Task<EmailVerificationToken?> GetEmailVerificationTokenByTokenHashAsync(string tokenHash)
        {
            var token = await context.EmailVerificationToken.Where(t => t.TokenHash == tokenHash).FirstOrDefaultAsync();
            return token;
        }

        public async Task<EmailVerificationToken?> ValidateTokenAsync(string rawToken)
        {
            var tokenHash = TokenGenerator.HashToken(rawToken);

            var token = await GetEmailVerificationTokenByTokenHashAsync(tokenHash);

            if (token == null || DateTime.UtcNow > token.ExpiresOnUtc) return null;

            return token;
        }

        public async Task AddEmailVerificationTokenAsync(EmailVerificationToken token)
        {
            await context.AddAsync(token);
        }
    }
}
