using ClientDashboard_API.Entities;
using System.Runtime.CompilerServices;

namespace ClientDashboard_API.Interfaces
{
    public interface IUserRepository
    {
        Task<UserBase?> GetUserByEmailAsync(string email);

        Task<UserBase?> GetUserByPasswordResetTokenAsync(int tokenId);

        void ChangeUserPassword(UserBase user, string newPassword);

        void ChangeUserNotificationStatus(UserBase user, bool status);
    }
}
