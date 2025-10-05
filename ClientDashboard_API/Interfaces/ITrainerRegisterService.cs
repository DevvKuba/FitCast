using ClientDashboard_API.DTOs;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerRegisterService
    {
        Task<TrainerServiceDto> Handle(RegisterDto registerDto);
    }
}
