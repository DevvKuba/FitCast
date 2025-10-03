using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerRepository
    {
        Task<Trainer?> GetTrainerByEmailAsync(string email);

        Task<Trainer?> GetTrainerByIdAsync(int id);

        Task AddNewTrainerAsync(Trainer trainer);

        void DeleteTrainer(Trainer trainer);

        Task<bool> DoesExistAsync(string email);

    }
}
