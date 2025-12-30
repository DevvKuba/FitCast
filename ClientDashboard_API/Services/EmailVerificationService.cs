using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using FluentEmail.Core;

namespace ClientDashboard_API.Services
{
    public class EmailVerificationService(IUnitOfWork unitOfWork, IEmailVerificationLinkFactory linkFactory, IFluentEmail fluentEmail) : IEmailVerificationService
    {
        public async Task CreateAndSendVerificationEmailAsync(Trainer trainer)
        {
            DateTime currentTime = DateTime.UtcNow;
            var verificationToken = new EmailVerificationToken
            {
                TrainerId = trainer.Id,
                CreatedOnUtc = currentTime,
                ExpiresOnUtc = currentTime.AddDays(1)
            };

            await unitOfWork.EmailVerificationTokenRepository.AddEmailVerificationTokenAsync(verificationToken);
            await unitOfWork.Complete();

            string verificationLink = linkFactory.Create(verificationToken);

            //email verification
            await fluentEmail
                .To(trainer.Email)
                .Subject("Email verification for FitCast")
                .Body($"To verify your email address <a href='{verificationLink}'>click here</a>", isHtml: true)
                .SendAsync();
        }
    }
}
