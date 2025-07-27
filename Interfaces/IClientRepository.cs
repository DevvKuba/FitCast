using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IClientRepository
    {
        // think of methods necessary to gather client data, within the ClientDataController
        Task<Client> GetClientByNameAsync(string clientName);

        Task<int> GetClientsCurrentSessionAsync(string name);

        Task<List<string>> GetClientsOnLastSessionAsync();

        Task<List<string>> GetClientsOnFirstSessionAsync();

        Task UpdateClientCurrentSessionAsync(string clientName);

        Task AddNewClientAsync(string clientName);

        void RemoveClient(Client client);

        Task<bool> CheckIfClientExistsAsync(string clientName);

        Task AddWorkoutAsync(Workout workout);

        Task RemoveWorkout(Workout workout);

    }
}
