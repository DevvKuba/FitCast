using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface IClientRepository
    {
        // think of methods necessary to gather client data, within the ClientDataController
        Task<List<Client>> GetAllTrainerClientDataAsync(int trainerId);

        Task<Client?> GetClientByNameWithTrainerAsync(Trainer trainer, string clientName);

        Task<Client?> GetClientByIdAsync(int? id);

        Task<Client?> GetClientByIdWithWorkoutsAsync(int id);

        Task<Client?> GetClientByIdWithTrainerAsync(int id);

        Task<Client?> GetClientByEmailAsync(string email);

        void UpdateClientDetailsAsync(Client client, ClientUpdateDto updatedClient);

        void UpdateClientDetailsUponRegisterationAsync(Trainer trainer, Client client, RegisterDto clientDetails);

        void UpdateAddingClientCurrentSessionAsync(Client client);

        void UpdateDeletingClientCurrentSession(Client client);

        void UnassignTrainerAsync(Client client);

        Task<Client?> AddNewClientUnderTrainerAsync(string clientName, int? blockSessions, string? phoneNumber, int? trainerId);

        Task<List<Client>> GetSoftDeletedClientsOlderThanAsync(DateTime cutoffDate);

        void RemoveClient(Client client);

        void SoftDeleteClientAsync(Client client);

        Task<bool> CheckIfClientExistsAsync(string clientName);


    }
}
