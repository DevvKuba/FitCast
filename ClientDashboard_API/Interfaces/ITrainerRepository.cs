using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerRepository
    {
        Task<Trainer?> GetTrainerByEmailAsync(string email);

        Task<Trainer?> GetTrainerWithClientsByIdAsync(int id);

        Task<Trainer?> GetTrainerByIdAsync(int id);

        Task<List<Client>> GetTrainerClientsAsync(Trainer trainer);

        Task UpdateTrainerPhoneNumberAsync(int trainerId, string phoneNumber);

        Task UpdateTrainerApiKeyAsync(int trainerId, string apiKey);

        void AssignClient(Trainer trainer, Client client);

        Task AddNewTrainerAsync(Trainer trainer);

        void DeleteTrainer(Trainer trainer);

        Task<bool> DoesExistAsync(string email);

    }
}
