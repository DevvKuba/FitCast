using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task<PasswordResetToken?> GetPasswordResetTokenByIdAsync(int tokenId);
        Task AddPasswordResetTokenAsync(PasswordResetToken token);
    }
}
