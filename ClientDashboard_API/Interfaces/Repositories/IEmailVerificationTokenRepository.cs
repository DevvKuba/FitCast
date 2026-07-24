using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface IEmailVerificationTokenRepository : ITokenRepository<EmailVerificationToken>
    {
        Task<EmailVerificationToken?> GetTokenByIdWithTrainerAsync(int tokenId);
    }
}
