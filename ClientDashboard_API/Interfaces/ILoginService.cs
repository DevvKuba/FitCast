using ClientDashboard_API.DTOs;

namespace ClientDashboard_API.Interfaces
{
    public interface ILoginService
    {
        Task<ApiResponseDto<UserDto>> Handle(LoginDto loginDto);
    }
}
