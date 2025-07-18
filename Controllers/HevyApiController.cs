using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class HevyApiController(ISessionDataParser hevyDataParser, ISessionSyncService syncService) : BaseAPIController
    {
        [HttpPost]
        public async Task<ActionResult> GatherAndUpdateDailySessions()
        {
            var result = await syncService.SyncDailySessions(hevyDataParser);
            if (!result) return BadRequest("incorrect process to gather & update daily sessions");
            return Ok(result);
        }
    }
}
