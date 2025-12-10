using ClientDashboard_API.DTOs;

namespace ClientDashboard_API.Interfaces
{
    public interface IRegisterService
    {
        Task<ApiResponseDto<string>> Handle(RegisterDto registerDto);

        Task<bool> MapClientDataUponRegistration(RegisterDto registerDto);
    }
}
