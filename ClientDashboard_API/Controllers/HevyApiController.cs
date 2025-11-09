using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class HevyApiController(ISessionSyncService syncService) : BaseAPIController
    {
        [HttpPut]
        public async Task<ActionResult> GatherAndUpdateDailySessions()
        {
            var result = await syncService.SyncDailyPipelineSessionsAsync();
            if (!result) return Ok("No workouts collected from Hevy App");
            return Ok(result);
        }
    }
}
