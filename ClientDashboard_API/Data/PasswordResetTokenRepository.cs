using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Data
{
    public class PasswordResetTokenRepository(DataContext context) : IPasswordResetTokenRepository
    {
        public async Task AddPasswordResetTokenAsync(PasswordResetToken token)
        {
            await context.AddAsync(token);
        }
    }
}
