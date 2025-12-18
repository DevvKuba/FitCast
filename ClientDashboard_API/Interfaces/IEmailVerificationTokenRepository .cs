using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IEmailVerificationTokenRepository
    {
        Task AddEmailVerificationTokenAsync(EmailVerificationToken token);

        Task<EmailVerificationToken?> GetEmailVerificationTokenByIdAsync(int tokenId);
    }
}
