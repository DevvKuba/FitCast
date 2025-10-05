using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class AccountController(IUnitOfWork unitOfWork, ITrainerRegisterService registerService, ITrainerLoginService loginService) : BaseAPIController
    {
        [HttpPost("/register")]
        public async Task<ActionResult<ApiResponseDto<string>>> Register([FromBody] RegisterDto registerInfo)
        {
            var trainer = await registerService.Handle(registerInfo);
            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"{trainer.FirstName} was not added sucessfully", Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"{trainer.FirstName} was successfuly added", Success = true });
        }

        [HttpPost("/login")]
        public async Task<ActionResult<ApiResponseDto<string>>> Login([FromBody] LoginDto loginInfo)
        {
            var response = await loginService.Handle(loginInfo);

            if (response.Data == null)
            {
                // both error cases return null , response.Message contains specific error message
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = response.Message, Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = response.Data, Message = "token created successfully, user now logged in", Success = true });

        }
    }
}
