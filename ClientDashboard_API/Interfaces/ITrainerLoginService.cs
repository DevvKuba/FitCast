using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerLoginService
    {
        Task<Trainer> Handle(LoginDto loginDto);
    }
}
