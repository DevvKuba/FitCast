using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Data
{
    public class EmailVerificationTokenRepository(DataContext context) : IEmailVerificationTokenRepository
    {
        public async Task AddEmailVerificationTokenAsync(EmailVerificationToken token)
        {
            await context.AddAsync(token);
        }
    }
}
