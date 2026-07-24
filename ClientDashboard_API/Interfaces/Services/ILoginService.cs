using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Services
{
    public interface ILoginService
    {
        Task<ApiResponseDto<UserDto>> Handle(LoginDto loginDto);
    }
}
