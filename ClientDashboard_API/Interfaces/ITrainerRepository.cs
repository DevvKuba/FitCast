using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerRepository
    {
        Task<Trainer?> GetTrainerByEmailAsync(string email);

        Task<Trainer?> GetTrainerByIdAsync(int id);

        Task<List<Client>> GetTrainerClientsAsync(Trainer trainer);

        void AssignClient(Trainer trainer, Client client);

        Task AddNewTrainerAsync(Trainer trainer);

        void DeleteTrainer(Trainer trainer);

        Task<bool> DoesExistAsync(string email);

    }
}
