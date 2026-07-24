using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Services
{
    public interface IPasswordResetService
    {
        Task CreateAndSendPasswordResetEmailAsync(UserBase user);
    }
}
