using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class UserRepository(DataContext context, IPasswordHasher passwordHasher) : IUserRepository
    {

        public async Task<UserBase?> GetUserByEmailAsync(string email)
        {
            return await context.Users.Where(u => u.Email == email).FirstOrDefaultAsync();
        }

        public async Task<UserBase?> GetUserByPasswordResetTokenAsync(int tokenId)
        {
            var token = await context.PasswordResetToken.Where(p => p.Id == tokenId).Include(p => p.User).FirstOrDefaultAsync();
            var user = token != null ? token.User : null;
            return user;
        }

        public void ChangeUserPassword(UserBase user, string newPassword)
        {
            user.PasswordHash = passwordHasher.Hash(newPassword);
        }
    }
}
