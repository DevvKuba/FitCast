using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IRegisterService
    {
        Task<ApiResponseDto<string>> Handle(RegisterDto registerDto);

        Task<bool> MapClientDataUponRegistrationAsync(RegisterDto registerDto);

        Task CreateAndSendVerificationEmailAsync(Trainer trainer);
    }
}
