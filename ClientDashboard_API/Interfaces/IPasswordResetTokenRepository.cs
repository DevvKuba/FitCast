using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task<PasswordResetToken?> GetPasswordResetTokenByIdAsync(int tokenId);

        Task<PasswordResetToken?> GetPasswordResetTokenByTokenHashAsync(string tokenHash);

        Task<PasswordResetToken?> ValidateTokenAsync(string tokenHash);

        Task AddPasswordResetTokenAsync(PasswordResetToken token);

        void ConsumeToken(PasswordResetToken token);
    }
}
