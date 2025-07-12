using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class HevyApiController(IUnitOfWork unitOfWork, ISessionDataParser parser) : BaseAPIController
    {
        [HttpPost]
        public async Task<ActionResult> GatherAndStoreDailyClientSesions()
        {
            List<WorkoutSummaryDto> workoutSummaries = await parser.CallApi();

            // gather each of the clients from the workoutSummaries
            var client = unitOfWork.ClientDataRepository.GetClientRecordByName("");
            return Ok();


        }


    }
}
