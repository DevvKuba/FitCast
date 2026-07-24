using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Services
{
    public interface IEmailVerificationService
    {
        Task CreateAndSendVerificationEmailAsync(Trainer trainer);
    }
}
