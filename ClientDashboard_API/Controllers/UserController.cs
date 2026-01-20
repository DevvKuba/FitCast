using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class UserController(IUnitOfWork unitOfWork) : BaseAPIController
    {
        [Authorize]
        [HttpPost("changeNotificationStatus")]
        public async Task<ActionResult<ApiResponseDto<string>>> ChangeUserNotificationStatusAsync([FromBody] NotificationStatusDto userInfo)
        {
            var user = await unitOfWork.UserRepository.GetUserByIdAsync(userInfo.Id);

            if(user is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "User was not found, notification status not changed", Success = false });
            }

            unitOfWork.UserRepository.ChangeUserNotificationStatus(user, userInfo.NotificationStatus);

            if(!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Changing notification status was unsuccessful", Success = false });
            }
            string statusTerm = user.NotificationsEnabled ? "enabled" : "disabled";

            return Ok(new ApiResponseDto<string> { Data = user.FirstName, Message = $"Notifications successfully {statusTerm}", Success = true });
        }

    }
}
