using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerRegisterService
    {
        Task<Trainer> Handle(RegisterDto registerDto);
    }
}
