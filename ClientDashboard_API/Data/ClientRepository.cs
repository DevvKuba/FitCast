using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;


namespace ClientDashboard_API.Data
{
    public class ClientRepository(DataContext context, IPasswordHasher passwordHasher, IMapper mapper) : IClientRepository
    {
        public async Task<List<Client>> GetAllTrainerClientDataAsync(int trainerId)
        {
            var clients = await context.Client.Where(x => x.TrainerId == trainerId)
                .OrderByDescending(x => x.IsActive)
                .ToListAsync();
            return clients;
        }

        public void UpdateAddingClientCurrentSessionAsync(Client client)
        {
            int newCurrentSession = client.CurrentBlockSession + 1;
            if (newCurrentSession > client.TotalBlockSessions)
            {
                newCurrentSession = 1;
            }

            var updatedData = new ClientUpdateDto
            {
                Id = client.Id,
                FirstName = client.FirstName,
                IsActive = client.IsActive,
                CurrentBlockSession = newCurrentSession,
                TotalBlockSessions = client.TotalBlockSessions
            };
            mapper.Map(updatedData, client);
        }

        public void UpdateClientDetailsUponRegisterationAsync(Trainer trainer, Client client, RegisterDto clientDetails)
        {
            client.Surname = clientDetails.Surname;
            client.Email = clientDetails.Email;
            client.PhoneNumber = clientDetails.PhoneNumber.Replace(" ", "");
            client.PasswordHash = passwordHasher.Hash(clientDetails.Password);
        }

        public void UpdateDeletingClientCurrentSession(Client client)
        {
            int newCurrentSession = client.CurrentBlockSession - 1;

            if (newCurrentSession == 0)
            {
                if (client.TotalBlockSessions is not null)
                {
                    newCurrentSession = (int)client.TotalBlockSessions;
                }

            }

            var updatedData = new ClientUpdateDto
            {
                Id = client.Id,
                FirstName = client.FirstName,
                IsActive = client.IsActive,
                CurrentBlockSession = newCurrentSession,
                TotalBlockSessions = client.TotalBlockSessions,
            };
            mapper.Map(updatedData, client);
        }

        public void UpdateClientDetailsAsync(Client client, ClientUpdateDto updatedClient)
        {
            mapper.Map(updatedClient, client);
        }

        public async Task<Client?> GetClientByNameWithTrainerAsync(Trainer trainer, string clientName)
        {
            var client = await context.Client
                .Where(c => c.FirstName == clientName.ToLower() &&
                c.TrainerId == trainer.Id)
                .Include(c => c.Trainer)
                .FirstOrDefaultAsync();
            return client;
        }

        public async Task<Client?> GetClientByIdAsync(int? id)
        {
            var clientData = await context.Client.Where(x => x.Id == id).FirstOrDefaultAsync();
            return clientData;
        }

        public async Task<Client?> GetClientByIdWithWorkoutsAsync(int id)
        {
            var client = await context.Client.Where(c => c.Id == id).Include(x => x.Workouts).FirstOrDefaultAsync();
            return client;

        }
        public async Task<Client?> GetClientByIdWithTrainerAsync(int id)
        {
            var client = await context.Client.Where(c => c.Id == id).Include(x => x.Trainer).FirstOrDefaultAsync();
            return client;
        }

        public async Task<Client?> GetClientByEmailAsync(string email)
        {
           var client = await context.Client.Where(c => c.Email == email).FirstOrDefaultAsync();
            return client;
        }

        public async Task<bool> CheckIfClientExistsAsync(string clientName)
        {
            return await context.Client.AnyAsync(record => record.FirstName == clientName.ToLower());
        }

        public void UnassignTrainerAsync(Client client)
        {
            client.TrainerId = null;
        }

        // TODO eventually when pipeline is no longer needed, maybe trainerId a non-nullable type
        public async Task<Client?> AddNewClientUnderTrainerAsync(string clientName, int? blockSessions, string? phoneNumber, int? trainerId)
        {
            var trainer = await context.Trainer.Where(x => x.Id == trainerId).FirstOrDefaultAsync();
            var newClient = new Client
            {
                FirstName = clientName.ToLower(),
                Role = Enums.UserRole.Client,
                IsActive = true,
                CurrentBlockSession = 0,
                TotalBlockSessions = blockSessions,
                PhoneNumber = phoneNumber?.Replace(" ", ""),
                TrainerId = trainerId,
                Trainer = trainer,
            };

            await context.Client.AddAsync(newClient);

            return newClient;
        }

        public async Task<List<Client>> GetSoftDeletedClientsOlderThanAsync(DateTime cutoffDate)
        {
            var clients = await context.Client
                .Where(c => c.IsDeleted && c.DeletedAt.HasValue && c.DeletedAt.Value <= cutoffDate)
                 .IgnoreQueryFilters()
                .ToListAsync();
            return clients;
        }

        public void SoftDeleteClientAsync(Client client)
        {
            client.IsDeleted = true;
            client.DeletedAt = DateTime.UtcNow;
        }

        public void RemoveClient(Client client)
        {
            context.Client.Remove(client);
        }

    }
}
