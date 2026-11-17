using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using System.Xml.Serialization;

namespace ClientDashboard_API.Interfaces.Repositories
{
    public interface ITrainerRepository
    {
        Task<Trainer?> GetTrainerByEmailAsync(string email);

        Task<Trainer?> GetTrainerByPhoneNumberAsync(string phoneNumber);

        Task<Trainer?> GetTrainerWithClientsByIdAsync(int id);

        Task<List<Client>> GatherDeletedTrainerClientsByTrainerIdAsync(int trainerId);

        Task<List<Trainer>> GetAllTrainersAsync();

        Task<List<Trainer>> GetAllTrainersEligibleForRevenueTrackingAsync();

        Task<Trainer?> GetTrainerByIdAsync(int id);

        Task<List<Client>> GetTrainerClientsWithWorkoutsAsync(Trainer trainer);

        Task<List<Client>> GetTrainerActiveClientsAsync(Trainer trainer);

        void UpdateTrainerProfileDetailsAsync(Trainer trainer, TrainerUpdateDto updateDto);

        Task UpdateTrainerPhoneNumberAsync(int trainerId, string phoneNumber);

        void UpdateTrainerPaymentSettingAsync(Trainer trainer, bool enabled);

        void AssignClient(Trainer trainer, Client client);

        Task AddNewTrainerAsync(Trainer trainer);

        void DeleteTrainer(Trainer trainer);

        Task<bool> DoesEmailExistAsync(string email);

        Task<bool> DoesPhoneNumberExistAsync(string phoneNumber);

    }
}
