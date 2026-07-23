using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace ClientDashboard_API.Data
{
    public class EmailVerificationTokenRepository(DataContext context): IEmailVerificationTokenRepository
    {
        public async Task<EmailVerificationToken?> GetTokenByIdWithTrainerAsync(int tokenId)
        {
            var token = await context.Set<EmailVerificationToken>()
                .Where(t => t.Id == tokenId)
                .Include(t => t.Trainer)
                .FirstOrDefaultAsync();
            return token;
        }
    }
}
