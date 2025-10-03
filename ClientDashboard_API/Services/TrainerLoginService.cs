using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class TrainerLoginService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher) : ITrainerLoginService
    {
        public async Task<Trainer> Handle(LoginDto loginDto)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByEmailAsync(loginDto.Email);

            if (trainer is null)
            {
                throw new Exception("The user was not found");
            }

            bool verified = passwordHasher.Verify(loginDto.Password, trainer.PasswordHash);

            if (!verified)
            {
                throw new Exception("The password is incorrect");
            }
            return trainer;
        }
    }
}
