using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class UserRepository(DataContext context) : IUserRepository
    {
        public async Task<UserBase?> GetUserByEmailAsync(string email)
        {
            return await context.Users.Where(u => u.Email == email).FirstOrDefaultAsync();
        }
    }
}
