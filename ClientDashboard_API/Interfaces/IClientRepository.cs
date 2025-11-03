using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IClientRepository
    {
        // think of methods necessary to gather client data, within the ClientDataController
        Task<List<Client>> GetAllTrainerClientDataAsync(int trainerId);
        Task<Client> GetClientByNameAsync(string clientName);

        Task<Client> GetClientByIdAsync(int? id);

        Task<int> GetClientsCurrentSessionAsync(string name);

        Task<List<string>> GetClientsOnLastSessionAsync();

        Task<List<string>> GetClientsOnFirstSessionAsync();

        void UpdateClientDetailsAsync(Client client, string newClientName, bool newActivity, int? newCurrentSession, int? newTotalSessions);

        void UpdateAddingClientCurrentSessionAsync(Client client);

        void UpdateDeletingClientCurrentSession(Client client);

        void UpdateClientTotalBlockSession(Client client, int? blockSessions);

        void UpdateClientCurrentSession(Client client, int? currentSession);

        void UpdateClientName(Client client, string name);

        void UnassignTrainerAsync(Client client);

        Task AddNewClientAsync(string clientName, int? blockSessions, int? trainerId);

        void RemoveClient(Client client);

        Task<bool> CheckIfClientExistsAsync(string clientName);


    }
}
