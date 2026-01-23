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
        [Authorize(Roles = "Trainer")]
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

        [Authorize(Roles = "Trainer")]
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

        [Authorize(Roles = "Trainer,Client")]
        [HttpGet("getNotificationStatus")]
        public async Task<ActionResult<ApiResponseDto<bool>>> GetUserNotificationStatusAsync([FromQuery] int userId)
        {
            var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);

            if (user is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "User was not found, notification status not retrived", Success = false });
            }
            return Ok(new ApiResponseDto<bool> { Data = user.NotificationsEnabled, Message = "Notification status successfully retrieved", Success = true });
        }

        [Authorize(Roles = "Trainer,Client")]
        [HttpPut("changeNotificationStatus")]
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

        [Authorize(Roles = "Trainer,Client")]
        [HttpGet("gatherUnreadUserNotifications")]
        public async Task<ActionResult<ApiResponseDto<List<Notification>>>> GatherUnreadUserNotificationsAsync([FromQuery] int userId)
        {
            var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);
            
            if(user is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "User was not found, cannot retrieve latest notificaitons", Success = false });
            }

            var unreadNotifications = new List<Notification>();

            if (user.Role == Enums.UserRole.Trainer)
            {
                unreadNotifications = await unitOfWork.NotificationRepository.ReturnUnreadTrainerNotifications(user);
            }
            else if(user.Role == Enums.UserRole.Client)
            {
                unreadNotifications = await unitOfWork.NotificationRepository.ReturnUnreadClientNotifications(user);
            }
            return Ok(new ApiResponseDto<List<Notification>> { Data = unreadNotifications, Message = "Successfully returned the latest notifications", Success = true });
        }

        // return set number of new notifications

    }
}
