using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class TrainerLoginService(IUnitOfWork unitOfWork, ITokenProvider tokenProvider, IPasswordHasher passwordHasher) : ITrainerLoginService
    {
        public async Task<TrainerServiceDto> Handle(LoginDto loginDto)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByEmailAsync(loginDto.Email);

            if (trainer is null)
            {
                return new TrainerServiceDto { Data = null, Message = "The user was not found" };
            }

            bool verified = passwordHasher.Verify(loginDto.Password, trainer.PasswordHash);

            if (!verified)
            {
                return new TrainerServiceDto { Data = null, Message = "The password is incorrect" };
            }

            var token = tokenProvider.Create(trainer);
            return new TrainerServiceDto { Data = token, Message = "Token created successfully" };
        }
    }
}
