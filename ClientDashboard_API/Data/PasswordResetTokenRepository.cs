using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Data
{
    // no password-reset-specific queries yet - all behaviour comes from the shared
    // TokenRepository<PasswordResetToken> base
    public class PasswordResetTokenRepository(DataContext context)
        : TokenRepository<PasswordResetToken>(context), IPasswordResetTokenRepository
    {
    }
}
