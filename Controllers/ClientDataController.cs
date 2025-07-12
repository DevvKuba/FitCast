
using ClientDashboard_API.Dto_s;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class ClientDataController() : BaseAPIController
    {
        [HttpGet]
        public async Task<List<WorkoutDataDto>> GetAllDailyClientSessions(string clientName)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<WorkoutDataDto> GetCurrentClientBlockSession(string clientName)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<WorkoutDataDto> GetLatestClientSessionDate(string clientName)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<List<string>> GetClientsOnLastSession()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<List<string>> GetClientsOnFirstSession()
        {
            throw new NotImplementedException();
        }

    }
}
