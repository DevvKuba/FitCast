using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Helpers
{
    internal sealed class VerifyEmail(DataContext context) : IVerifyEmail
    {
        public async Task<bool> Handle(int tokenId)
        {
            EmailVerificationToken? token = await context.EmailVerificationToken
                .Include(e => e.Trainer)
                .FirstOrDefaultAsync(e => e.Id == tokenId);

            if(token is null || token.ExpiresOnUtc < DateTime.UtcNow || token.Trainer!.EmailVerified)
            {
                return false;
            }

            token.Trainer.EmailVerified = true;

            context.EmailVerificationToken.Remove(token);

            await context.SaveChangesAsync();

            return true;
        } 
    }
}
