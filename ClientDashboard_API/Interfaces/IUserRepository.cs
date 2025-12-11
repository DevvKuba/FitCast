using ClientDashboard_API.Entities;
using System.Runtime.CompilerServices;

namespace ClientDashboard_API.Interfaces
{
    public interface IUserRepository
    {
        Task<UserBase?> GetUserByEmailAsync(string email);
    }
}
