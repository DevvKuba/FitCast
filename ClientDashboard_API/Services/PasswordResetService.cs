using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using FluentEmail.Core;
using System.Security.Cryptography;

namespace ClientDashboard_API.Services
{
    public class PasswordResetService(IUnitOfWork unitOfWork, IPasswordResetLinkFactory linkFactory, IFluentEmail fluentEmail) : IPasswordResetService
    {
        public async Task CreateAndSendPasswordResetEmailAsync(UserBase user)
        {
            DateTime currentTime = DateTime.UtcNow;
            var rawToken = TokenGenerator.GenerateToken();

            var passwordResetToken = new PasswordResetToken
            {
                TokenHash = TokenGenerator.HashToken(rawToken),
                CreatedOnUtc = currentTime,
                ExpiresOnUtc = currentTime.AddDays(1),
                UserId = user.Id
            };

            await unitOfWork.PasswordResetTokenRepository.AddPasswordResetTokenAsync(passwordResetToken);
            await unitOfWork.Complete();

            string resetRedirectionLink = linkFactory.Create(rawToken);

            await fluentEmail
               .To(user.Email)
               .Subject("Password Reset for FitCast")
               .Body($"To reset your existing password <a href='{resetRedirectionLink}'>click here</a>", isHtml: true)
               .SendAsync();
        }
    }
}
