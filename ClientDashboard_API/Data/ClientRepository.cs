using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

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
                FirstName = client.FirstName,
                IsActive = client.IsActive,
                CurrentBlockSession = newCurrentSession,
                TotalBlockSessions = client.TotalBlockSessions,
            };
            mapper.Map(updatedData, client);
        }

        public void UpdateClientDetailsAsync(Client client, string newClientName, bool newActivity, int? newCurrentSession, int? newTotalSessions)
        {
            var updatedData = new ClientUpdateDto
            {
                FirstName = newClientName.ToLower(),
                IsActive = newActivity,
                CurrentBlockSession = newCurrentSession,
                TotalBlockSessions = newTotalSessions
            };
            mapper.Map(updatedData, client);
        }

        public void UpdateClientTotalBlockSession(Client client, int? blockSessions)
        {
            var updatedData = new ClientUpdateDto
            {
                FirstName = client.FirstName,
                IsActive = client.IsActive,
                TotalBlockSessions = blockSessions,
                CurrentBlockSession = client.CurrentBlockSession

            };
            mapper.Map(updatedData, client);
        }

        public void UpdateClientCurrentSession(Client client, int? currentSession)
        {
            var updatedData = new ClientUpdateDto
            {
                FirstName = client.FirstName,
                IsActive = client.IsActive,
                TotalBlockSessions = client.TotalBlockSessions,
                CurrentBlockSession = currentSession
            };
            mapper.Map(updatedData, client);
        }

        public void UpdateClientName(Client client, string name)
        {
            var updatedData = new ClientUpdateDto
            {
                FirstName = name,
                IsActive = client.IsActive,
                TotalBlockSessions = client.TotalBlockSessions,
                CurrentBlockSession = client.CurrentBlockSession,
            };
            mapper.Map(updatedData, client);
        }

        public void UpdateClientPhoneNumber(Client client, string phoneNumber)
        {
            client.PhoneNumber = client.PhoneNumber!.Replace(" ", "");
        }

        public async Task<Client?> GetClientByNameAsync(string clientName)
        {
            var clientData = await context.Client.Where(x => x.FirstName == clientName.ToLower()).FirstOrDefaultAsync();
            return clientData;
        }

        public async Task<Client?> GetClientByNameUnderTrainer(Trainer trainer, string clientName)
        {
            var client = await context.Client.Where(c => c.FirstName == clientName.ToLower() && c.TrainerId == trainer.Id).FirstOrDefaultAsync();
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

        public async Task<int?> GetClientsCurrentSessionAsync(string clientName)
        {
            var clientData = await context.Client.Where(x => x.FirstName == clientName.ToLower()).Select(x => x.CurrentBlockSession).FirstOrDefaultAsync();
            return clientData;
        }


        public async Task<List<string>> GetClientsOnFirstSessionAsync()
        {
            var clients = await context.Client.Where(x => x.CurrentBlockSession == 1).Select(x => x.FirstName.ToLower()).ToListAsync();
            return clients;
        }

        public async Task<List<string>> GetClientsOnLastSessionAsync()
        {
            var clients = await context.Client.Where(x => x.CurrentBlockSession == x.TotalBlockSessions).Select(x => x.FirstName.ToLower()).ToListAsync();
            return clients;
        }

        public async Task<bool> CheckIfClientExistsAsync(string clientName)
        {
            return await context.Client.AnyAsync(record => record.FirstName == clientName.ToLower());
        }

        public int GatherDailyClientStepsAsync(Client client)
        {
            return client.DailySteps;
        }

        public void UnassignTrainerAsync(Client client)
        {
            client.TrainerId = null;
        }

        // eventually when pipeline is no longer needed, maybe trainerId a non-nullable type
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

        public async Task<Client?> AddNewClientUserAsync(Client client, int trainerId)
        {
            var trainer = await context.Trainer.Where(x => x.Id == trainerId).FirstOrDefaultAsync();
            var newClient = new Client
            {
                FirstName = client.FirstName.ToLower(),
                Role = Enums.UserRole.Client,
                Surname = client.Surname ?? "".ToLower(),
                PhoneNumber = client.PhoneNumber?.Replace(" ", ""),
                IsActive = true,
                TrainerId = trainerId,
                Trainer = trainer,
            };

            await context.Client.AddAsync(newClient);

            return newClient;
        }

        public void RemoveClient(Client client)
        {
            context.Client.Remove(client);
        }

    }
}
