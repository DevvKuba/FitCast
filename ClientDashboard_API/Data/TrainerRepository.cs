using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class TrainerRepository(DataContext context) : ITrainerRepository
    {
        public async Task<Trainer?> GetTrainerByEmailAsync(string email)
        {
            var trainer = await context.Trainer.Where(x => x.Email == email).FirstOrDefaultAsync();
            return trainer;
        }

        public async Task<Trainer?> GetTrainerByIdAsync(int id)
        {
            var trainer = await context.Trainer
                //.Include(t => t.Clients)
                .Where(x => x.Id == id).FirstOrDefaultAsync();
            return trainer;
        }

        public async Task<List<Client>> GetTrainerClientsAsync(Trainer trainer)
        {
            var clientList = await context.Trainer
                //.Include(x => x.Clients)
                .SelectMany(x => x.Clients.Where(x => x.TrainerId == trainer.Id)).ToListAsync();
            return clientList;
        }

        public void AssignClient(Trainer trainer, Client client)
        {
            client.TrainerId = trainer.Id;
            client.Trainer = trainer;
        }

        public async Task AddNewTrainerAsync(Trainer trainer)
        {
            await context.Trainer.AddAsync(trainer);
        }

        public void DeleteTrainer(Trainer trainer)
        {
            context.Trainer.Remove(trainer);
        }

        public async Task<bool> DoesExistAsync(string email)
        {
            return await context.Trainer.AnyAsync(x => x.Email == email);
        }
    }
}
