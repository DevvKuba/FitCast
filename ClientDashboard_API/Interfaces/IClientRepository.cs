using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IClientRepository
    {
        // think of methods necessary to gather client data, within the ClientDataController
        Task<List<Client>> GetAllClientDataAsync();
        Task<Client> GetClientByNameAsync(string clientName);

        Task<int> GetClientsCurrentSessionAsync(string name);

        Task<List<string>> GetClientsOnLastSessionAsync();

        Task<List<string>> GetClientsOnFirstSessionAsync();

        void UpdateAddingClientCurrentSessionAsync(Client client);

        void UpdateDeletingClientCurrentSession(Client client);

        void UpdateClientTotalBlockSession(Client client, int? blockSessions);

        void UpdateClientCurrentSession(Client client, int? currentSession);

        void UpdateClientName(Client client, string name);

        Task AddNewClientAsync(string clientName, int? blockSessions);

        void RemoveClient(Client client);

        Task<bool> CheckIfClientExistsAsync(string clientName);


    }
}
