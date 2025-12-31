using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using FluentEmail.Core;

namespace ClientDashboard_API.Services
{
    public class PasswordResetService(IUnitOfWork unitOfWork, IPasswordResetLinkFactory linkFactory, IFluentEmail fluentEmail) : IPasswordResetService
    {
        public Task CreateAndSendPasswordResetEmailAsync(UserBase user)
        {
            //DateTime currentTime = DateTime.UtcNow;
            //var passwordResetToken = new PasswordResetToken
            //{
            //    CreatedOnUtc = currentTime,
            //    ExpiresOnUtc = currentTime.AddDays(1),
            //    UserId = user.Id,
            //}

            throw new NotImplementedException();
        }
    }
}
