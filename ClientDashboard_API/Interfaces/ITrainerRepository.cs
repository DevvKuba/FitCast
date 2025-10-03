using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerRepository
    {
        Task<Trainer?> GetTrainerByEmail(string email);

        Task<Trainer?> GetTrainerById(int id);

        void AddNewTrainer(Trainer trainer);

        void DeleteTrainer(Trainer trainer);

    }
}
