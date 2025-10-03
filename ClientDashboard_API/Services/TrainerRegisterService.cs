using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;


namespace ClientDashboard_API.Services
{
    public sealed class TrainerRegisterService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher) : ITrainerRegisterService
    {

        public async Task<Trainer> Handle(RegisterDto request)
        {
            if (await unitOfWork.TrainerRepository.DoesExistAsync(request.Email))
            {
                throw new Exception("The email is already in use");
            }

            var trainer = new Trainer
            {
                Email = request.Email,
                FirstName = request.FirstName,
                Surname = request.Surname,
                PasswordHash = passwordHasher.Hash(request.Password)
            };

            await unitOfWork.TrainerRepository.AddNewTrainerAsync(trainer);

            // email verification ?
            // access token 

            return trainer;
        }
    }
}
