using ClientDashboard_API.DTOs;

namespace ClientDashboard_API.Interfaces
{
    public interface IClientDailyFeatureRepository
    {
        Task AddNewRecordAsync(ClientDailyDataAddDto clientData);
    }
}
