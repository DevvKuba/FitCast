using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Helpers
{
    public interface IClientBlockTerminationHelper
    {
        Task<ApiResponseDto<string>> CreateAllAdequateEntityReminderAsync(Client client);
    }
}
