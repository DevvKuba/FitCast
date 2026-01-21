using ClientDashboard_API.Data;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Twilio.Rest.Api.V2010.Account.Sip.Domain.AuthTypes.AuthTypeCalls;

namespace ClientDashboard_API.Controllers
{
    public class NotificationController(IUnitOfWork unitOfWork, INotificationService notificationService) : BaseAPIController
    {
        [Authorize(Roles = "trainer")]
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

        [Authorize(Roles = "trainer")]
        [HttpPost("SendClientBlockCompletionReminder")]
        public async Task<ActionResult<ApiResponseDto<string>>> ClientBlockCompletionReminderAsync(int trainerId, int clientId)
        {
            var messageResponse = await notificationService.SendClientReminderAsync(trainerId, clientId);

            if (messageResponse.Success == false)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = messageResponse.Message, Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = messageResponse.Data, Message = messageResponse.Message, Success = true });

        }

        [Authorize(Roles = "trainer,client")]
        [HttpPost("changeNotificationStatus")]
        public async Task<ActionResult<ApiResponseDto<string>>> ChangeUserNotificationStatusAsync([FromBody] NotificationStatusDto userInfo)
        {
            var user = await unitOfWork.UserRepository.GetUserByIdAsync(userInfo.Id);

            if (user is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "User was not found, notification status not changed", Success = false });
            }

            unitOfWork.UserRepository.ChangeUserNotificationStatus(user, userInfo.NotificationStatus);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Changing notification status was unsuccessful", Success = false });
            }
            string statusTerm = user.NotificationsEnabled ? "enabled" : "disabled";

            return Ok(new ApiResponseDto<string> { Data = user.FirstName, Message = $"Notifications successfully {statusTerm}", Success = true });
        }

        // return latest 10 notifications 
        [Authorize(Roles = "trainer,client")]
        [HttpPost("GatherLatestUserNotifications")]
        public async Task<ActionResult<ApiResponseDto<List<Notification>>>> GatherLatestUserNotificationsAsync([FromQuery] int userId)
        {
            var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);
            
            if(user is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "User was not found, cannot retrieve latest notificaitons", Success = false });
            }

            var latestNotifications = new List<Notification>();

            if (user.Role == "trainer")
            {
                latestNotifications = await unitOfWork.NotificationRepository.ReturnLatestTrainerNotifications(user);
            }
            else if(user.Role == "client")
            {
                latestNotifications = await unitOfWork.NotificationRepository.ReturnLatestClientNotifications(user);
            }
            return Ok(new ApiResponseDto<List<Notification>> { Data = latestNotifications, Message = "Successfully returned the latest notifications", Success = true });

        }


        // return set number of new notifications

    }
}
