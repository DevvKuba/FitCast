using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IClientDataRepository
    {
        // think of methods necessary to gather client data, within the ClientDataController

        Task<WorkoutData> GetClientsLastSessionAsync(string name);

        Task<List<WorkoutData>> GetClientRecordsByDateAsync(DateOnly date);

        Task<List<string>> GetClientsOnLastSessionAsync();

        Task<List<string>> GetClientsOnFirstSessionAsync();

        Task UpdateClientCurrentSessionAsync(string clientName);

        Task AddNewClientAsync(string clientName, DateOnly sessionDate);

        Task<bool> CheckIfClientExistsAsync(string clientName);

    }
}
