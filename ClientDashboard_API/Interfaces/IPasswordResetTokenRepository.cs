using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task<PasswordResetToken?> GetPasswordResetTokenByIdAsync(int tokenId);

        Task<PasswordResetToken?> GetPasswordResetTokenByTokenHashAsync(string tokenHash);

        Task<List<EmailVerificationToken>> GetAllExpiredOrConsumedTokensAsync();

        Task<PasswordResetToken?> ValidateTokenAsync(PasswordResetToken token);

        Task AddPasswordResetTokenAsync(PasswordResetToken token);

        void ConsumeToken(PasswordResetToken token);

        void RemoveToken(PasswordResetToken token);
    }
}
