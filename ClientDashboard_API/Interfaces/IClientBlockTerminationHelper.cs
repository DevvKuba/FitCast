using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IClientBlockTerminationHelper
    {
        Task CreateAdequateRemindersAndPaymentsAsync(Client client);
    }
}
