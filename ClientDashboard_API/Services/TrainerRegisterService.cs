
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
            if (request.FirstName.Length is 0 || request.Surname.Length is 0 || request.Password.Length is 0 
                || request.Email.Length is 0 || request.PhoneNumber.Length is 0 || request.UserType is null)
            {
                return new ApiResponseDto<string> { Data = null, Message = "Must fill in all required fields", Success = false };
            }

            if (await unitOfWork.TrainerRepository.DoesEmailExistAsync(request.Email))
            {
                return new ApiResponseDto<string> { Data = null, Message = "The email is already in use", Success = false };
            }

            if(await unitOfWork.TrainerRepository.DoesPhoneNumberExistAsync(request.PhoneNumber))
            {
                return new ApiResponseDto<string> { Data = null, Message = "The phone number is already is use", Success = false };
            }

            // email / sms verification step

            if (request.UserType == "trainer")
            {
                var trainer = new Trainer
                {
                    FirstName = request.FirstName,
                    Surname = request.Surname,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber.Replace(" ", ""),
                    PasswordHash = passwordHasher.Hash(request.Password)
                };

                await unitOfWork.TrainerRepository.AddNewTrainerAsync(trainer);
                return new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"{trainer.FirstName} successfully added", Success = true };
            }
            else
            {
                // instead of actually creating a new client we will map the provided data over to the identified client upon verifying (via trainer phone number and client name)
                var result = await MapClientDataUponRegistration(request);

                if (!result)
                {
                    return new ApiResponseDto<string> { Data = "Error", Message = "Unsuccessful processing of client link to trainer", Success = false };
                }

                return new ApiResponseDto<string> { Data = "Success", Message = $"Successfully registered as a client", Success = true };
            }
        }

        public async Task<bool> MapClientDataUponRegistration(RegisterDto request)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(request.ClientsTrainerId);

            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(request.ClientId);

            if(trainer == null || client == null)
            {
                return false;
            }

            unitOfWork.ClientRepository.UpdateClientDetailsUponRegisterationAsync(trainer, client, request);
            return true;
        }
    }
}
