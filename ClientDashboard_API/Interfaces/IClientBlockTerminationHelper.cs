using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IClientBlockTerminationHelper
    {
        Task<ApiResponseDto<string>> CreateAdequateTrainerRemindersAndPaymentsAsync(Client client);
    }
}
