using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IAutoPaymentCreationService
    {
        Task CreatePendingPaymentAsync(Trainer trainer, Client client);
    }
}
