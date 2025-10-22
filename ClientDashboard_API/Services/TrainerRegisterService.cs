
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;


namespace ClientDashboard_API.Services
{
    public sealed class TrainerRegisterService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher) : ITrainerRegisterService
    {

        public async Task<ApiResponseDto<string>> Handle(RegisterDto request)
        {
            // check if any fields are empty
            if (request.FirstName.Length is 0 || request.Surname.Length is 0 || request.Password.Length is 0 || request.Email.Length is 0)
            {
                return new ApiResponseDto<string> { Data = null, Message = "Must fill in all required fields", Success = false };
            }

            if (await unitOfWork.TrainerRepository.DoesExistAsync(request.Email))
            {
                return new ApiResponseDto<string> { Data = null, Message = "The email is already in use", Success = false };
            }

            var trainer = new Trainer
            {
                Email = request.Email,
                FirstName = request.FirstName,
                Surname = request.Surname,
                PasswordHash = passwordHasher.Hash(request.Password)
            };

            await unitOfWork.TrainerRepository.AddNewTrainerAsync(trainer);

            // email verification 

            return new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"{trainer.FirstName} successfully added", Success = true };
        }
    }
}
