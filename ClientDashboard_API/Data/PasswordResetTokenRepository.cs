using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class PasswordResetTokenRepository(DataContext context) : IPasswordResetTokenRepository
    {
        public async Task<PasswordResetToken?> GetPasswordResetTokenByIdAsync(int tokenId)
        {
            return await context.PasswordResetToken.Where(t => t.Id == tokenId).FirstOrDefaultAsync();
        }
        public async Task AddPasswordResetTokenAsync(PasswordResetToken token)
        {
            await context.AddAsync(token);
        }
    }
}
