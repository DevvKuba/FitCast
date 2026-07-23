using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IEmailVerificationTokenRepository
    {
        Task<EmailVerificationToken?> GetTokenByIdWithTrainerAsync(int tokenId);
    }
}
