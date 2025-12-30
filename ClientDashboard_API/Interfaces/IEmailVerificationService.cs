using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IEmailVerificationService
    {
        Task CreateAndSendVerificationEmailAsync(Trainer trainer);
    }
}
