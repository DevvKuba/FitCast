using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class TrainerRepository(DataContext context, IMapper mapper) : ITrainerRepository
    {
        public async Task<Trainer?> GetTrainerByEmailAsync(string email)
        {
            var trainer = await context.Trainer.Where(x => x.Email == email).FirstOrDefaultAsync();
            return trainer;
        }

        public async Task<Trainer?> GetTrainerByPhoneNumberAsync(string phoneNumber)
        {
            var flatPhoneNumber = phoneNumber.Replace(" ", "");

            var trainer = await context.Trainer.Where(t => t.PhoneNumber == flatPhoneNumber).FirstOrDefaultAsync();
            return trainer;
        }

        public async Task<Trainer?> GetTrainerByIdAsync(int id)
        {
            var trainer = await context.Trainer.Where(x => x.Id == id).FirstOrDefaultAsync();
            return trainer;
        }

        public async Task<List<Trainer>> GetAllTrainersAsync()
        {
            return await context.Trainer.ToListAsync();
        }

        public async Task<List<Trainer>> GetAllTrainersEligibleForRevenueTrackingAsync()
        {
            var cutOffDate = DateTime.UtcNow.AddDays(-14);

            return await context.Trainer.Where(t => t.CreatedAt <= cutOffDate)
                .Include(t => t.Clients)
                .ThenInclude(c => c.Workouts)
                .ToListAsync();
        }


        public async Task<Trainer?> GetTrainerWithClientsByIdAsync(int id)
        {
            var trainer = await context.Trainer
                .Include(t => t.Clients)
                .Where(x => x.Id == id).FirstOrDefaultAsync();

            return trainer;
        }

        public async Task<List<Trainer>> GetTrainersWithAutoRetrievalAsync()
        {
            var trainers = await context.Trainer.Where(x => x.AutoWorkoutRetrieval == true).ToListAsync();
            return trainers;
        }

        public async Task<List<Client>> GetTrainerClientsWithWorkoutsAsync(Trainer trainer)
        {
            var clientList = await context.Client
                .Include(x => x.Workouts)
                .Where(x => x.Trainer == trainer)
                .ToListAsync();
            return clientList;
        }

        public async Task<List<Client>> GetTrainerActiveClientsAsync(Trainer trainer)
        {
            var clientList = await context.Client
                .Include(x => x.Workouts)
                .Where(x => x.Trainer == trainer
                 && x.IsActive == true)
                .ToListAsync();
            return clientList;
        }

        public void AssignClient(Trainer trainer, Client client)
        {
            client.TrainerId = trainer.Id;
        }

        public void UpdateTrainerProfileDetailsAsync(Trainer trainer, TrainerUpdateDto updateDto)
        {
            mapper.Map(updateDto, trainer);
        }

        public async Task UpdateTrainerPhoneNumberAsync(int trainerId, string phoneNumber)
        {
            var trainer = await GetTrainerByIdAsync(trainerId);
            trainer!.PhoneNumber = phoneNumber;
        }

        public void UpdateTrainerAutoRetrievalAsync(Trainer trainer, bool enabled)
        {
            trainer.AutoWorkoutRetrieval = enabled;
        }

        public void UpdateTrainerPaymentSettingAsync(Trainer trainer, bool enabled)
        {
            trainer.AutoPaymentSetting = enabled;
        }

        public void UpdateTrainerApiKeyAsync(Trainer trainer, string apiKey)
        {
            trainer.WorkoutRetrievalApiKey = apiKey;
        }

        public async Task AddNewTrainerAsync(Trainer trainer)
        {
            await context.Trainer.AddAsync(trainer);
        }

        public void DeleteTrainer(Trainer trainer)
        {
            context.Trainer.Remove(trainer);
        }

        public async Task<bool> DoesEmailExistAsync(string email)
        {
            return await context.Trainer.AnyAsync(x => x.Email == email);
        }

        public async Task<bool> DoesPhoneNumberExistAsync(string phoneNumber)
        {
            var flatPhoneNumber = phoneNumber.Replace(" ", "");

            return await context.Trainer.AnyAsync(t => t.PhoneNumber == flatPhoneNumber);
        }

    }
}
