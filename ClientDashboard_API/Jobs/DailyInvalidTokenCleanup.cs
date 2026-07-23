using ClientDashboard_API.Interfaces;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyInvalidTokenCleanup(IUnitOfWork unitOfWork, ILogger<DailyInvalidTokenCleanup> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("Daily invalid token cleaup STARTING");

            var invalidPasswordResetTokens = await unitOfWork.PasswordResetTokenRepository.GetAllExpiredOrConsumedTokensAsync();

            logger.LogInformation("Retrieved: {tokenCount} invalid password reset tokens", invalidPasswordResetTokens.Count);

            var invalidEmailVerificationTokens = await unitOfWork.EmailVerificationTokenRepository.GetAllExpiredOrConsumedTokensAsync();

            logger.LogInformation("Retrieved: {tokenCount} invalid email verification tokens", invalidEmailVerificationTokens.Count);

            foreach (var passwordToken in invalidPasswordResetTokens)
            {
                unitOfWork.PasswordResetTokenRepository.RemoveToken(passwordToken);
            }

            foreach (var emailToken in invalidEmailVerificationTokens)
            {
                unitOfWork.EmailVerificationTokenRepository.RemoveToken(emailToken);
            }

            await unitOfWork.Complete();

            var newInvalidPasswordResetTokens = await unitOfWork.PasswordResetTokenRepository.GetAllExpiredOrConsumedTokensAsync();

            var newInvalidEmailVerificationTokens = await unitOfWork.EmailVerificationTokenRepository.GetAllExpiredOrConsumedTokensAsync();

            if (newInvalidPasswordResetTokens.Count != 0 || newInvalidEmailVerificationTokens.Count != 0)
            {
                logger.LogError("Not all invalid tokens have been removed");
            }
            else
            {
                logger.LogInformation("Invalid token removed process complete");
            }

        }
    }
}
