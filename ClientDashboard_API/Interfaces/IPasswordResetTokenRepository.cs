using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task AddPasswordResetTokenAsync(PasswordResetToken token);
    }
}
