using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class AccountController(IUnitOfWork unitOfWork, ITrainerRegisterService registerService, ITrainerLoginService loginService) : BaseAPIController
    {
        [HttpPost("/register")]
        public async Task<ActionResult<ApiResponseDto<string>>> Register(RegisterDto registerInfo)
        {
            var trainer = await registerService.Handle(registerInfo);
            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"{trainer.FirstName} was not added sucessfully", Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"{trainer.FirstName} was successfuly added", Success = true });
        }

        [HttpPost("/login")]
        public async Task<ActionResult<ApiResponseDto<string>>> Login(LoginDto loginInfo)
        {
            var trainer = await loginService.Handle(loginInfo);

            // rework to call exception if the password doesn't become verified
            // if (!await unitOfWork.Complete())
            // {
            //    return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"{trainer.FirstName} didn't log in successfully", Success = false });
            // }



            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"{trainer.FirstName} logged in successfully", Success = true });


        }
    }
}
