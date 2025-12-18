using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class AccountController(IUnitOfWork unitOfWork, IRegisterService registerService, ILoginService loginService) : BaseAPIController
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

            if (user.Data is null)
            {
                // both error cases return null , response.Message contains specific error message
                return NotFound(new ApiResponseDto<UserDto> { Data = null, Message = user.Message, Success = false });
            }

            return Ok(new ApiResponseDto<UserDto> { Data = user.Data, Message = "token created successfully, user now logged in", Success = true });

        }
        [AllowAnonymous]
        [HttpGet("verify-email/{token}", Name = "VerifyEmail")]
        public async Task<ActionResult<ApiResponseDto<string>>> VerifyEmailVerificationTokenAsync( token)
        {

        }

        [AllowAnonymous]
        [HttpGet("verifyClientUnderTrainer")]
        public async Task<ActionResult<ApiResponseDto<ClientVerificationInfoDto>>> VerfiyClientsTrainerStatusAsync([FromQuery] string trainerPhoneNumber, [FromQuery] string clientFirstName)
        {
            // checking if both trainer exists and if the client firstName is currently present under that trainer

            var trainer = await unitOfWork.TrainerRepository.GetTrainerByPhoneNumberAsync(trainerPhoneNumber);

            if(trainer is null)
            {
                return NotFound(new ApiResponseDto<ClientVerificationInfoDto> { Data = null, Message = $"Trainer not found for client {clientFirstName}", Success = false });
            }

            var client = await unitOfWork.ClientRepository.GetClientByNameUnderTrainer(trainer, clientFirstName);

            if(client is null)
            {
                return NotFound(new ApiResponseDto<ClientVerificationInfoDto> { Data = null, Message = $"Trainer does not have client under the name: ${clientFirstName}", Success = false });
            }

            var clientVerifiedInfo = new ClientVerificationInfoDto { ClientId = client.Id, TrainerId = trainer.Id };

            return Ok(new ApiResponseDto<ClientVerificationInfoDto> { Data = clientVerifiedInfo, Message = $"Client: {clientFirstName} found under Trainer: {trainer.FirstName}", Success = true });
        }
    }
}
