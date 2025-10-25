using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    [Authorize]
    public class TrainerController(IUnitOfWork unitOfWork, IMapper mapper) : BaseAPIController
    {
        /// <summary>
        /// Trainer method allowing assignment of client under them
        /// </summary>
        [HttpPut("assignClient")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateClientAssignmentAsync([FromQuery] int clientId, [FromQuery] int trainerId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);

            if (client == null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "client does not exist", Success = false });
            }

            unitOfWork.TrainerRepository.AssignClient(trainer, client);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving date when updating clients trainer", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = client.Name, Message = $"client: {client.Name} is now under trainer: {trainer.FirstName}", Success = true });
        }


    }
}
