using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IPasswordResetService
    {
        Task CreateAndSendPasswordResetEmailAsync(UserBase user);
    }
}
