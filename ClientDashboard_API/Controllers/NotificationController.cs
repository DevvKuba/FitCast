using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    [Authorize(Roles = "trainer")]
    public class NotificationController(INotificationService notificationService) : BaseAPIController
    {
        [HttpPost("SendTrainerBlockCompletionReminder")]
        public async Task<ActionResult<ApiResponseDto<string>>> TrainerBlockCompletionReminderAsync(int trainerId, int clientId)
        {
            var messageResponse = await notificationService.SendTrainerReminderAsync(trainerId, clientId);

            if (messageResponse.Success == false)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = messageResponse.Message, Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = messageResponse.Data, Message = messageResponse.Message, Success = true });

        }

        [HttpPost("SendClientBlockCompletionReminder")]
        public async Task<ActionResult<ApiResponseDto<string>>> ClientBlockCompletionReminderAsync(int trainerId, int clientId)
        {
            var messageResponse = await notificationService.SendTrainerReminderAsync(trainerId, clientId);

            if (messageResponse.Success == false)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = messageResponse.Message, Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = messageResponse.Data, Message = messageResponse.Message, Success = true });

        }

    }
}
