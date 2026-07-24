using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Services
{
    public interface IClientDailyFeatureService
    {
        Task ExecuteClientDailyGatheringAsync(Client client);
    }
}
