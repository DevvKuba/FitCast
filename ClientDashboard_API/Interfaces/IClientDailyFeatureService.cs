using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IClientDailyFeatureService
    {
        Task ExecuteClientDailyGatheringAsync(Client client);
    }
}
