using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using FluentEmail.Core;

namespace ClientDashboard_API.Services
{
    public class PasswordResetService(IUnitOfWork unitOfWork, IPasswordResetLinkFactory linkFactory, IFluentEmail fluentEmail) : IPasswordResetService
    {
        public async Task CreateAndSendPasswordResetEmailAsync(UserBase user)
        {
            DateTime currentTime = DateTime.UtcNow;
            var passwordResetToken = new PasswordResetToken
            {
                CreatedOnUtc = currentTime,
                ExpiresOnUtc = currentTime.AddDays(1),
                UserId = user.Id
            };

            await unitOfWork.PasswordResetTokenRepository.AddPasswordResetTokenAsync(passwordResetToken);
            await unitOfWork.Complete();

            string resetRedirectionLink = linkFactory.Create(passwordResetToken);

            await fluentEmail
               .To(user.Email)
               .Subject("Password Reset for FitCast")
               .Body($"To reset your existing password <a href='{resetRedirectionLink}'>click here</a>", isHtml: true)
               .SendAsync();
        }
    }
}
