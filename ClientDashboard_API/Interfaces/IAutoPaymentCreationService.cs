using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IAutoPaymentCreationService
    {
        Task<ApiResponseDto<string>> CreatePendingPaymentAsync(Trainer trainer, Client client);
    }
}
