using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class EmailVerificationTokenRepository(DataContext context) : IEmailVerificationTokenRepository
    {
        public async Task<EmailVerificationToken?> GetEmailVerificationTokenByIdAsync(int tokenId)
        {
            var token = await context.EmailVerificationToken.Where(t => t.Id == tokenId).FirstOrDefaultAsync();
            return token;
        }

        public async Task AddEmailVerificationTokenAsync(EmailVerificationToken token)
        {
            await context.AddAsync(token);
        }

    }
}
