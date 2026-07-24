using ClientDashboard_API.DTOs;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface IClientDailyFeatureRepository
    {
        Task AddNewRecordAsync(ClientDailyDataAddDto clientData);
    }
}
