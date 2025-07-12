using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Controllers
{
    public class ClientDataController(DbContext context) : BaseAPIController
    {
        [HttpGet]
        public async Task<List<WorkoutData>> GetAllDailyClientSessions()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<WorkoutSummaryDto> GetClientCurrentBlockSession(string clientName)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<WorkoutSummaryDto> GetLatestClientSessionDates(string clientName)
        {
            throw new NotImplementedException();
        }
    }
}
