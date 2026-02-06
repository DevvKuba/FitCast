using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IClientBlockTerminationHelper
    {
        Task CreateAdequateTrainersRemindersAndPaymentsAsync(Client client);
    }
}
