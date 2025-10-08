using ClientDashboard_API.DTOs;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerLoginService
    {
        Task<ApiResponseDto<UserDto>> Handle(LoginDto loginDto);
    }
}
