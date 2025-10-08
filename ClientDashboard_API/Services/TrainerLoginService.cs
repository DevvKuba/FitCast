using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class TrainerLoginService(IUnitOfWork unitOfWork, ITokenProvider tokenProvider, IPasswordHasher passwordHasher) : ITrainerLoginService
    {
        public async Task<ApiResponseDto<UserDto>> Handle(LoginDto loginDto)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByEmailAsync(loginDto.Email);

            if (trainer is null)
            {
                return new ApiResponseDto<UserDto> { Data = null, Message = "The user was not found", Success = false };
            }

            bool verified = passwordHasher.Verify(loginDto.Password, trainer.PasswordHash);

            if (!verified)
            {
                return new ApiResponseDto<UserDto> { Data = null, Message = "The password is incorrect", Success = false };
            }

            var token = tokenProvider.Create(trainer);

            var user = new UserDto { FirstName = trainer.FirstName, Token = token };

            return new ApiResponseDto<UserDto> { Data = user, Message = "Token created successfully", Success = true };
        }
    }
}
