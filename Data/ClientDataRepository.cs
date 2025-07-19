using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class ClientDataRepository(DataContext context, IMapper mapper) : IClientDataRepository
    {
        public async Task UpdateClientCurrentSessionAsync(string clientName)
        {
            var clientData = await GetClientsLastSessionAsync(clientName);
            int newCurrentSession = clientData.CurrentBlockSession + 1;
            if (newCurrentSession > clientData.TotalBlockSessions) newCurrentSession = 1;

            var updatedData = new WorkoutUpdateDto
            {
                CurrentBlockSession = newCurrentSession,
            };

            mapper.Map(updatedData, clientData);


        }

        public async Task<WorkoutData> GetClientsLastSessionAsync(string clientName)
        {
            var clientData = await context.Data.OrderByDescending(x => x.SessionDate).Where(x => x.Title == clientName).FirstOrDefaultAsync();
            return clientData;
        }

        public async Task<List<WorkoutData>> GetClientRecordsByDateAsync(DateOnly date)
        {
            var clientData = await context.Data.Where(x => x.SessionDate == date).ToListAsync();
            return clientData;
        }

        public async Task<List<string>> GetClientsOnFirstSessionAsync()
        {
            var clients = await context.Data.Where(x => x.CurrentBlockSession == 1).Select(x => x.Title).ToListAsync();
            return clients;
        }

        public async Task<List<string>> GetClientsOnLastSessionAsync()
        {
            var clients = await context.Data.Where(x => x.CurrentBlockSession == x.TotalBlockSessions).Select(x => x.Title).ToListAsync();
            return clients;
        }

        public async Task AddNewClientAsync(string clientName, DateOnly sessionDate)
        {
            // entity state? can you just add it
            await context.Data.AddAsync(new WorkoutData
            {
                Title = clientName,
                SessionDate = sessionDate,
                CurrentBlockSession = 1,
                TotalBlockSessions = null
            });
        }

        public async Task<bool> CheckIfClientExistsAsync(string clientName)
        {
            return await context.Data.AnyAsync(record => record.Title == clientName);
        }
    }
}
