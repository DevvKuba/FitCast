using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using Twilio.TwiML.Voice;
using Twilio.Types;

namespace ClientDashboard_API.Controllers
{
    [Authorize]
    public class TrainerController(IUnitOfWork unitOfWork, IMapper mapper, IApiKeyEncryter encrypter, ISessionDataParser hevyDataParser, ISessionSyncService syncService) : BaseAPIController
    {
        /// <summary>
        /// Trainer method allowing for the retrieval of a specific Trainer by id
        /// </summary>
        [HttpGet("retrieveTrainerById")]
        public async Task<ActionResult<ApiResponseDto<Trainer>>> RetrieveTrainerByIdAsync([FromQuery] int trainerId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return BadRequest(new ApiResponseDto<Trainer> { Data = null, Message = "trainer does not exist", Success = false });
            }

            return Ok(new ApiResponseDto<Trainer> { Data = trainer, Message = $"trainer: {trainer.FirstName} successfully retrieved", Success = true });
        }

        /// <summary>
        /// Trainer method allowing assignment of client under them
        /// </summary>
        [HttpPut("updateTrainerProfileDetails")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateTrainerProfileAsync([FromQuery] int trainerId, [FromBody] TrainerUpdateDto updatedTrainerProfile)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return BadRequest(new ApiResponseDto<Trainer> { Data = null, Message = "trainer does not exist", Success = false });
            }

            unitOfWork.TrainerRepository.UpdateTrainerProfileDetailsAsync(trainer, updatedTrainerProfile);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "error saving trainer profile", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"trainer: {trainer.FirstName} has had their profile updated successfully", Success = true });
        }

        /// <summary>
        /// Trainer method allowing assignment of client under them
        /// </summary>
        [HttpPut("assignClient")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateClientAssignmentAsync([FromQuery] int clientId, [FromQuery] int trainerId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerWithClientsByIdAsync(trainerId);

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
        [HttpPut("updateTrainerPhoneNumber")]
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
            unitOfWork.TrainerRepository.UpdateTrainerApiKeyAsync(trainer, encryptedApiKey);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"error saving {trainer.FirstName}'s new api key", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"trainer: {trainer.FirstName}'s api key successfully set up", Success = true });
        }

        /// <summary>
        /// Trainer method updating a Workout Retrieval Api Key, along with the toggle status of AutoRetrival
        /// </summary>
        [HttpPut("updateTrainerRetrievalDetails")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateTrainerRetrievalDetailsAsync([FromQuery] int trainerId, [FromQuery] string providedApiKey, [FromQuery] bool enabled)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            if (!await hevyDataParser.IsApiKeyValidAsync(providedApiKey))
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"provided api key: {providedApiKey} is not valid.", Success = false });
            }

            var encryptedApiKey = encrypter.Encrypt(providedApiKey);

            unitOfWork.TrainerRepository.UpdateTrainerApiKeyAsync(trainer, encryptedApiKey);
            unitOfWork.TrainerRepository.UpdateTrainerAutoRetrievalAsync(trainer, enabled);


            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"error saving {trainer.FirstName}'s new api key and auto retrieval status", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = trainer.FirstName, Message = $"trainer: {trainer.FirstName}'s api key and auto retrieval status successfully set up", Success = true });

        }


        //summary>
        /// Trainer method to collect daily client workout's from Hevy Workout Tracker
        /// </summary>
        [HttpPut("getDailyHevyWorkouts")]
        public async Task<ActionResult<ApiResponseDto<int>>> GatherAndUpdateHevyClientWorkoutsAsync([FromQuery] int trainerId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return BadRequest(new ApiResponseDto<int> { Data = 0, Message = "trainer does not exist", Success = false });
            }

            if (trainer.WorkoutRetrievalApiKey == null)
            {
                return BadRequest(new ApiResponseDto<int> { Data = 0, Message = "trainer does not have an assigned api key ", Success = false });
            }

            var collectedSessions = await syncService.SyncSessionsAsync(trainer);

            if (collectedSessions == 0)
            {
                return Ok(new ApiResponseDto<int> { Data = 0, Message = "No Hevy Workouts were collected", Success = true });
            }
            return Ok(new ApiResponseDto<int> { Data = collectedSessions, Message = $"{collectedSessions} Hevy workouts successfully retrieved", Success = true });
        }

        //summary>
        /// Trainer method to retrieve a trainer's Hevy Api Key
        /// </summary>
        [HttpGet("getHevyApiKey")]
        public async Task<ActionResult<ApiResponseDto<string>>> GetWorkoutRetrievalApiKeyAsync([FromQuery] int trainerId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            var encryptedApiKey = trainer.WorkoutRetrievalApiKey;

            if (encryptedApiKey == null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"trainer: {trainer.FirstName} does not have an api key set", Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = encrypter.Decrypt(trainer.WorkoutRetrievalApiKey!), Message = $"trainer: {trainer.FirstName}'s api key decrypted and returned successfully", Success = true });
        }

        //summary>
        /// Trainer method to retrieve a trainer's Auto Retrieval status
        /// </summary>
        [HttpGet("getAutoRetrievalStatus")]
        public async Task<ActionResult<ApiResponseDto<bool>>> GetAutoRetrievalStatusAsync([FromQuery] int trainerId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            return Ok(new ApiResponseDto<bool> { Data = trainer.AutoWorkoutRetrieval, Message = $"trainer: {trainer.FirstName}'s auto retrieval status enquired successfully", Success = true });
        }

        //summary>
        /// Trainer method to retrieve a trainer's Auto Payment Setting status
        /// </summary>
        [HttpGet("getAutoPaymentSettingStatus")]
        public async Task<ActionResult<ApiResponseDto<bool>>> GetAutoPaymentSettingStatusAsync([FromQuery] int trainerId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(trainerId);

            if (trainer == null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "trainer does not exist", Success = false });
            }

            return Ok(new ApiResponseDto<bool> { Data = trainer.AutoPaymentSetting, Message = $"trainer: {trainer.FirstName}'s auto retrieval status enquired successfully", Success = true });
        }
    }
}
