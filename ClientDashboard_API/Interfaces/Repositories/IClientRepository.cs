using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface IClientRepository
    {
        // think of methods necessary to gather client data, within the ClientDataController
        Task<List<Client>> GetAllTrainerClientDataAsync(int trainerId);
        Task<Client?> GetClientByNameAsync(string clientName);

        Task<Client?> GetClientByNameUnderTrainer(Trainer trainer, string clientName);

        Task<Client?> GetClientByIdAsync(int? id);

        Task<Client?> GetClientByIdWithWorkoutsAsync(int id);

        Task<Client?> GetClientByIdWithTrainerAsync(int id);

        Task<Client?> GetClientByEmailAsync(string email);

        void UpdateClientPhoneNumber(Client client, string phoneNumber);

        void UpdateClientDetailsAsync(Client client, string newClientName, bool newActivity, int? newCurrentSession, int? newTotalSessions, string? phoneNumber);

        void UpdateClientDetailsUponRegisterationAsync(Trainer trainer, Client client, RegisterDto clientDetails);

        void UpdateAddingClientCurrentSessionAsync(Client client);

        void UpdateDeletingClientCurrentSession(Client client);

        void UpdateClientTotalBlockSession(Client client, int? blockSessions);

        void UpdateClientCurrentSession(Client client, int? currentSession);

        void UpdateClientName(Client client, string name);

        void UnassignTrainerAsync(Client client);

        Task<Client?> AddNewClientUnderTrainerAsync(string clientName, int? blockSessions, string? phoneNumber, int? trainerId);

        Task<List<Client>> GetSoftDeletedClientsOlderThanAsync(DateTime cutoffDate);

        void RemoveClient(Client client);

        void SoftDeleteClientAsync(Client client);

        Task<bool> CheckIfClientExistsAsync(string clientName);


    }
}
