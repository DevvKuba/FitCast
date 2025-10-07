using ClientDashboard_API.DTOs;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerRegisterService
    {
        Task<ApiResponseDto<string>> Handle(RegisterDto registerDto);
    }
}
