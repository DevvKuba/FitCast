using ClientDashboard_API.DTOs;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerLoginService
    {
        Task<ApiResponseDto<string>> Handle(LoginDto loginDto);
    }
}
