using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class ClientRepository(DataContext context, IMapper mapper) : IClientRepository
    {
        public async Task<List<Client>> GetAllClientDataAsync()
        {
            var clients = await context.Client.ToListAsync();
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
                Name = client.Name,
                CurrentBlockSession = newCurrentSession,
                TotalBlockSessions = client.TotalBlockSessions
            };
            mapper.Map(updatedData, client);

        }

        public void UpdateClientDetailsAsync(Client client, string newClientName, int? newCurrentSession, int? newTotalSessions)
        {
            var updatedData = new ClientUpdateDto
            {
                Name = newClientName,
                CurrentBlockSession = newCurrentSession,
                TotalBlockSessions = newTotalSessions
            };
            mapper.Map(updatedData, client);
        }

        public void UpdateDeletingClientCurrentSession(Client client)
        {
            int newCurrentSession = client.CurrentBlockSession - 1;

            var updatedData = new ClientUpdateDto
            {
                CurrentBlockSession = newCurrentSession,
            };
            mapper.Map(updatedData, client);
        }

        public void UpdateClientTotalBlockSession(Client client, int? blockSessions)
        {
            var updatedData = new ClientUpdateDto
            {
                Name = client.Name,
                TotalBlockSessions = blockSessions,
                CurrentBlockSession = client.CurrentBlockSession
            };
            mapper.Map(updatedData, client);
        }

        public void UpdateClientCurrentSession(Client client, int? currentSession)
        {
            var updatedData = new ClientUpdateDto
            {
                Name = client.Name,
                TotalBlockSessions = client.TotalBlockSessions,
                CurrentBlockSession = currentSession
            };
            mapper.Map(updatedData, client);
        }

        public void UpdateClientName(Client client, string name)
        {
            var updatedData = new ClientUpdateDto
            {
                Name = name,
                TotalBlockSessions = client.TotalBlockSessions,
                CurrentBlockSession = client.CurrentBlockSession,
            };
            mapper.Map(updatedData, client);
        }

        public async Task<Client> GetClientByNameAsync(string clientName)
        {
            var clientData = await context.Client.Where(x => x.Name == clientName.ToLower()).FirstOrDefaultAsync();
            return clientData;
        }

        public async Task<Client> GetClientByIdAsync(int id)
        {
            var clientData = await context.Client.Where(x => x.Id == id).FirstOrDefaultAsync();
            return clientData;
        }

        public async Task<int> GetClientsCurrentSessionAsync(string clientName)
        {
            var clientData = await context.Client.Where(x => x.Name == clientName.ToLower()).Select(x => x.CurrentBlockSession).FirstOrDefaultAsync();
            return clientData;
        }


        public async Task<List<string>> GetClientsOnFirstSessionAsync()
        {
            var clients = await context.Client.Where(x => x.CurrentBlockSession == 1).Select(x => x.Name.ToLower()).ToListAsync();
            return clients;
        }

        public async Task<List<string>> GetClientsOnLastSessionAsync()
        {
            var clients = await context.Client.Where(x => x.CurrentBlockSession == x.TotalBlockSessions).Select(x => x.Name.ToLower()).ToListAsync();
            return clients;
        }

        public async Task AddNewClientAsync(string clientName, int? blockSessions)
        {
            await context.Client.AddAsync(new Client
            {

                Name = clientName.ToLower(),
                CurrentBlockSession = 0,
                TotalBlockSessions = blockSessions
            });
        }

        public void RemoveClient(Client client)
        {
            context.Client.Remove(client);
        }

        public async Task<bool> CheckIfClientExistsAsync(string clientName)
        {
            return await context.Client.AnyAsync(record => record.Name == clientName.ToLower());
        }

    }
}
