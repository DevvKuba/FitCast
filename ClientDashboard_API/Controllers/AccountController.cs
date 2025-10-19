using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class AccountController(IUnitOfWork unitOfWork, ITrainerRegisterService registerService, ITrainerLoginService loginService) : BaseAPIController
    {
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponseDto<string>>> Register([FromBody] RegisterDto registerInfo)
        {
            var response = await registerService.Handle(registerInfo);
            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = response.Message, Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = response.Data, Message = response.Message, Success = true });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponseDto<UserDto>>> Login([FromBody] LoginDto loginInfo)
        {
            var user = await loginService.Handle(loginInfo);

            if (user.Data == null)
            {
                // both error cases return null , response.Message contains specific error message
                return BadRequest(new ApiResponseDto<UserDto> { Data = null, Message = user.Message, Success = false });
            }

            return Ok(new ApiResponseDto<UserDto> { Data = user.Data, Message = "token created successfully, user now logged in", Success = true });

        }
    }
}
