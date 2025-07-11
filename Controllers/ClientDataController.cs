using ClientDashboard_API.Dto_s;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Controllers
{
    public class ClientDataController(DbContext context) : BaseAPIController
    {
        [HttpGet]
        public async Task<List<WorkoutSummaryDto>> GetDailyClientSessions()
        {

        }
    }
}
