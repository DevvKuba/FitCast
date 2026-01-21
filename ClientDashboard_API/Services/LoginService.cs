using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using System.Diagnostics.Contracts;
using Twilio.Rest.PreviewIam.Organizations;

namespace ClientDashboard_API.Services
{
    public class LoginService(IUnitOfWork unitOfWork, ITokenProvider tokenProvider, IPasswordHasher passwordHasher) : ILoginService
    {
        public async Task<ApiResponseDto<UserDto>> Handle(LoginDto loginDto)
        {
            var user = await unitOfWork.UserRepository.GetUserByEmailAsync(loginDto.Email);

            if (user is null)
            {
                return new ApiResponseDto<UserDto> { Data = null, Message = "The user was not found", Success = false };
            }

            if (loginDto.Role == Enums.UserRole.Trainer)
            {
                var trainer = await unitOfWork.TrainerRepository.GetTrainerByEmailAsync(loginDto.Email);
                if (!trainer!.EmailVerified)
                {
                    return new ApiResponseDto<UserDto> { Data = null, Message = "You must verifiy your email, you can resend the verification below", Success = false };
                }
            }
            else if(loginDto.Role is not Enums.UserRole.Trainer && loginDto.Role is not Enums.UserRole.Client)
            {
                return new ApiResponseDto<UserDto> { Data = null, Message = "User role type is not provided", Success = false };
            }

            bool verified = passwordHasher.Verify(loginDto.Password, user.PasswordHash!);

            if (!verified)
            {
                return new ApiResponseDto<UserDto> { Data = null, Message = "The password is incorrect", Success = false };
            }

            var token = tokenProvider.Create(user);

            var userDto = new UserDto { FirstName = user.FirstName, Id = user.Id, Token = token, Role = loginDto.Role };

            return new ApiResponseDto<UserDto> { Data = userDto, Message = "Token created successfully", Success = true };
        }

    }
}
