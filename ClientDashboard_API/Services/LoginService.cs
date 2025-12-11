using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using System.Diagnostics.Contracts;

namespace ClientDashboard_API.Services
{
    public class LoginService(IUnitOfWork unitOfWork, ITokenProvider tokenProvider, IPasswordHasher passwordHasher) : ILoginService
    {
        public async Task<ApiResponseDto<UserDto>> Handle(LoginDto loginDto)
        {
            if(loginDto.UserType == "trainer")
            {
                var trainer = await unitOfWork.TrainerRepository.GetTrainerByEmailAsync(loginDto.Email);

                if (trainer is null)
                {
                    return new ApiResponseDto<UserDto> { Data = null, Message = "The trainer user was not found", Success = false };
                }
            }
            else
            {
                var client = await unitOfWork.ClientRepository.GetClientByEmailAsync(loginDto.Email);

                if (client is null)
                {
                    return new ApiResponseDto<UserDto> { Data = null, Message = "The client user was not found", Success = false };
                }
            }



            bool verified = passwordHasher.Verify(loginDto.Password, trainer.PasswordHash!);

            if (!verified)
            {
                return new ApiResponseDto<UserDto> { Data = null, Message = "The password is incorrect", Success = false };
            }

            var token = tokenProvider.Create(trainer);

            var user = new UserDto { FirstName = trainer.FirstName, Id = trainer.Id, Token = token };

            return new ApiResponseDto<UserDto> { Data = user, Message = "Token created successfully", Success = true };
        }

    }
}
