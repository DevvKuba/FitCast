using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IEmailVerificationTokenRepository
    {

        Task<EmailVerificationToken?> GetEmailVerificationTokenByIdAsync(int tokenId);

        Task<EmailVerificationToken?> GetEmailVerificationTokenByTokenHashAsync(string tokenHash);

        Task<EmailVerificationToken?> ValidateTokenAsync(string rawToken);

        Task AddEmailVerificationTokenAsync(EmailVerificationToken token);
    }
}
