
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IClientDataRepository
    {
        // think of methods necessary to gather client data, within the ClientDataController

        Task<WorkoutData> GetClientRecordByName(string name);

        Task<List<WorkoutData>> GetClientRecordsByDate(DateOnly date);

        Task<List<string>> GetClientsOnLastSession();

        Task<List<string>> GetClientsOnFirstSession();
    }
}
