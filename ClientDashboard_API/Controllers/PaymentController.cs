using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace ClientDashboard_API.Controllers
{
    public class PaymentController(IUnitOfWork unitOfWork) : BaseAPIController
    {
        [HttpGet("getAllTrainerPayments")]
        public async Task<ActionResult<ApiResponseDto<string>>> getTrainerPaymentsAsync([FromQuery] int trainerId)
        {

        }
    }
}
