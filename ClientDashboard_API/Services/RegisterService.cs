
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using FluentEmail.Core;


namespace ClientDashboard_API.Services
{
    public sealed class RegisterService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IFluentEmail fluentEmail) : IRegisterService
    {

        public async Task<ApiResponseDto<string>> Handle(RegisterDto request)
        {
            // check if any fields are empty
            if (request.FirstName.Length is 0 || request.Surname.Length is 0 || request.Password.Length is 0 
                || request.Email.Length is 0 || request.PhoneNumber.Length is 0 || request.Role is null)
            {
                return new ApiResponseDto<string> { Data = null, Message = "Must fill in all required fields", Success = false };
            }

            if (await unitOfWork.TrainerRepository.DoesEmailExistAsync(request.Email))
            {
                return new ApiResponseDto<string> { Data = null, Message = "The email is already in use", Success = false };
            }

            // shouldn't apply to client - since the trainer might have already set up their number
            if(await unitOfWork.TrainerRepository.DoesPhoneNumberExistAsync(request.PhoneNumber))
            {
                return new ApiResponseDto<string> { Data = null, Message = "The phone number is already is use", Success = false };
            }

            // email / sms verification step

            if (request.Role == "trainer")
            {
                var trainer = new Trainer
                {
                    FirstName = request.FirstName,
                    Surname = request.Surname,
                    Role = request.Role,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber.Replace(" ", ""),
                    PasswordHash = passwordHasher.Hash(request.Password)
                };

                await unitOfWork.TrainerRepository.AddNewTrainerAsync(trainer);

                DateTime currentTime = DateTime.UtcNow;
                var verificationToken = new EmailVerificationToken
                {
                    TrainerId = trainer.Id,
                    CreatedOnUtc = currentTime,
                    // TODO may need to change
                    ExpiresOnUtc = currentTime.AddDays(1)
                };

                string verificationLink = "";

                //email verification
                await fluentEmail
                    .To(trainer.Email)
                    .Subject("Email verification for FitCast")
                    .Body($"To verify your email address <a href='{verificationLink}'>click here</a>", isHtml: true)
                    .SendAsync();

                return new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"{trainer.FirstName} successfully added", Success = true };
            }
            else
            {
                // instead of actually creating a new client we will map the provided data over to the identified client upon verifying (via trainer phone number and client name)
                if(request.ClientId != null && request.ClientsTrainerId != null)
                {
                    var result = await MapClientDataUponRegistration(request);

                    if (!result)
                    {
                        return new ApiResponseDto<string> { Data = "Error", Message = "Unsuccessful processing of client link to trainer", Success = false };
                    }
                    else
                    {
                        return new ApiResponseDto<string> { Data = "Success", Message = $"Successfully registered as a client", Success = true };
                    }
                }
                return new ApiResponseDto<string> { Data = "Error", Message = "clientId or clientsTrainerId are null fields", Success = false };
            }
        }

        public async Task<bool> MapClientDataUponRegistration(RegisterDto request)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(request.ClientsTrainerId ?? 0);

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
