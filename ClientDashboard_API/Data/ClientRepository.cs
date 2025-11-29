using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class ClientRepository(DataContext context, IMapper mapper) : IClientRepository
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
                FirstName = newClientName,
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
            client.PhoneNumber = phoneNumber;
        }

        public async Task<Client?> GetClientByNameAsync(string clientName)
        {
            var clientData = await context.Client.Where(x => x.FirstName == clientName.ToLower()).FirstOrDefaultAsync();
            return clientData;
        }

        public async Task<Client?> GetClientByIdAsync(int? id)
        {
            var clientData = await context.Client.Where(x => x.Id == id).FirstOrDefaultAsync();
            return clientData;
        }
        public async Task<Client?> GetClientByIdWithTrainerAsync(int id)
        {
            var client = await context.Client.Where(c => c.Id == id).Include(x => x.Trainer).FirstOrDefaultAsync();
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

        public void UnassignTrainerAsync(Client client)
        {
            client.TrainerId = null;
        }

        // eventually when pipeline is no longer needed, maybe trainerId a non-nullable type
        public async Task<Client?> AddNewClientAsync(string clientName, int? blockSessions, string? phoneNumber, int? trainerId)
        {
            var trainer = await context.Trainer.Where(x => x.Id == trainerId).FirstOrDefaultAsync();
            var newClient = new Client
            {
                FirstName = clientName.ToLower(),
                IsActive = true,
                CurrentBlockSession = 0,
                TotalBlockSessions = blockSessions,
                PhoneNumber = phoneNumber,
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
