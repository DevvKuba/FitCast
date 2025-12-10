using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerRepository
    {
        Task<Trainer?> GetTrainerByEmailAsync(string email);

        Task<Trainer?> GetTrainerByPhoneNumberAsync(string phoneNumber);

        Task<Trainer?> GetTrainerWithClientsByIdAsync(int id);

        Task<List<Trainer>> GetAllTrainersAsync();

        Task<List<Trainer>> GetAllTrainersEligibleForRevenueTrackingAsync();

        Task<Trainer?> GetTrainerByIdAsync(int id);

        Task<List<Trainer>> GetTrainersWithAutoRetrievalAsync();

        Task<List<Client>> GetTrainerClientsAsync(Trainer trainer);

        Task<List<Client>> GetTrainerActiveClientsAsync(Trainer trainer);

        void UpdateTrainerProfileDetailsAsync(Trainer trainer, TrainerUpdateDto updateDto);

        Task UpdateTrainerPhoneNumberAsync(int trainerId, string phoneNumber);

        void UpdateTrainerAutoRetrievalAsync(Trainer trainer, bool enabled);

        void UpdateTrainerPaymentSettingAsync(Trainer trainer, bool enabled);

        void UpdateTrainerApiKeyAsync(Trainer trainer, string apiKey);

        void AssignClient(Trainer trainer, Client client);

        Task AddNewTrainerAsync(Trainer trainer);

        void DeleteTrainer(Trainer trainer);

        Task<bool> DoesEmailExistAsync(string email);

        Task<bool> DoesPhoneNumberExistAsync(string phoneNumber);

    }
}
