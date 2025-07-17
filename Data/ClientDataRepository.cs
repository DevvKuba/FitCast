using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class ClientDataRepository(DataContext context) : IClientDataRepository
    {
        public async Task<WorkoutData> GetClientRecordByName(string clientName)
        {
            var clientData = await context.Data.Where(x => x.Title == clientName).FirstOrDefaultAsync();
            return clientData;
        }

        public async Task<List<WorkoutData>> GetClientRecordsByDate(DateOnly date)
        {
            var clientData = await context.Data.Where(x => x.SessionDate == date).ToListAsync();
            return clientData;
        }

        public async Task<List<string>> GetClientsOnFirstSession()
        {
            var clients = await context.Data.Where(x => x.CurrentBlockSession == 0).Select(x => x.Title).ToListAsync();
            return clients;
        }

        public async Task<List<string>> GetClientsOnLastSession()
        {
            var clients = await context.Data.Where(x => x.CurrentBlockSession == x.TotalBlockSessions).Select(x => x.Title).ToListAsync();
            return clients;
        }

    }
}
