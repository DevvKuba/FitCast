using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    [Authorize]
    public class TrainerController(IUnitOfWork unitOfWork, IMapper mapper, IApiKeyEncryter encrypter, ISessionDataParser hevyDataParser) : BaseAPIController
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
            return Ok(new ApiResponseDto<string> { Data = client.FirstName, Message = $"client: {client.FirstName} is now under trainer: {trainer.FirstName}", Success = true });
        }

        /// <summary>
        /// Trainer method allowing assignment of a new phone number
        /// </summary>
        [HttpPut("updateTrainerNumber")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdatePhoneNumberAsync([FromQuery] int trainerId, [FromQuery] string phoneNumber)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            await unitOfWork.TrainerRepository.UpdateTrainerPhoneNumberAsync(trainer.Id, phoneNumber);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"error saving {trainer.FirstName}'s new phone number", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"trainer: {trainer.FirstName}'s phone number updated to: {trainer.PhoneNumber}", Success = true });
        }

        /// <summary>
        /// Trainer method allowing assignment of a new Workout Retrieval Api Key
        /// </summary>
        [HttpPut("updateTrainerApiKey")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateWorkoutRetrievalApiKeyAsync([FromQuery] int trainerId, [FromQuery] string providedApiKey)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }
            // have a dummy / test method within HevySessionDataService can uses the apiKey to try and get a 200 response 

            if (!await hevyDataParser.IsApiKeyValidAsync(providedApiKey))
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"provided api key: {providedApiKey} is not valid.", Success = false });
            }

            // if that's the case use encryption service to encrypt the functioning key
            var encryptedApiKey = encrypter.Encrypt(providedApiKey);

            // store for trainer RetrievalWorkoutApiKey property
            await unitOfWork.TrainerRepository.UpdateTrainerApiKeyAsync(trainerId, encryptedApiKey);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"error saving {trainer.FirstName}'s new api key", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"trainer: {trainer.FirstName}'s api ket successfully set up", Success = true });
        }

    }
}
