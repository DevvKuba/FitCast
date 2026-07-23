using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Helpers
{
    internal sealed class VerifyEmail(IUnitOfWork unitOfWork) : IVerifyEmail
    {
        public async Task<bool> Handle(int tokenId)
        {
            EmailVerificationToken? token = await unitOfWork.EmailVerificationTokenRepository.GetTokenByIdWithTrainerAsync(tokenId);

            if (token is null || token.ExpiresOnUtc < DateTime.UtcNow || token.Trainer!.EmailVerified)
            {
                return false;
            }

            token.Trainer.EmailVerified = true;

            token.Consume();

            await unitOfWork.Complete();

            return true;
        } 
    }
}
