using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class EmailVerificationTokenRepository(DataContext context)
        : TokenRepository<EmailVerificationToken>(context), IEmailVerificationTokenRepository
    {
        public async Task<EmailVerificationToken?> GetTokenByIdWithTrainerAsync(int tokenId)
        {
            var token = await Entities
                .Where(t => t.Id == tokenId)
                .Include(t => t.Trainer)
                .FirstOrDefaultAsync();
            return token;
        }
    }
}
