using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class HevyApiController(IUnitOfWork unitOfWork, ISessionDataParser parser) : BaseAPIController
    {
        [HttpGet]
        public async Task<ActionResult> GatherDailyClientSessions()
        {

        }
    }
}
