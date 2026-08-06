using AutoMapper;
using ClientDashboard_API.Authorization;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace ClientDashboard_API.Controllers
{
    [Authorize]
    public class WorkoutController(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IClientBlockTerminationHelper clientBlockTerminator,
        IMapper mapper,
        IAuthorizationService authorizationService,
        ICurrentUserAccessor currentUserAccessor
        ) : BaseAPIController
    {

        [Authorize(Roles = "Client")]
        [HttpGet("GetClientSpecificWorkouts")]
        public async Task<ActionResult<ApiResponseDto<List<Workout>>>> GetClientSpecificWorkouts([FromQuery] int clientId)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdWithWorkoutsAsync(clientId);
            if (client is null)
            {
                return NotFound(new ApiResponseDto<List<Workout>> { Data = [], Message = "No clients with that id found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<List<Workout>> { Data = [], Message = "Not authorized to view this client's workouts", Success = false });
            }

            var clientWorkouts = await unitOfWork.WorkoutRepository.GetClientWorkoutsAsync(client);

            if (!clientWorkouts.Any())
            {
                return Ok(new ApiResponseDto<List<Workout>> { Data = [], Message = "No workout's found", Success = true });
            }

            return Ok(new ApiResponseDto<List<Workout>> { Data = clientWorkouts, Message = " workouts returned", Success = true });
        }

        /// <summary>
        /// Workout request for the retrieval of paginated workoutsb
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpGet("GetTrainerWorkouts")]
        public async Task<ActionResult<ApiResponseDto<List<Workout>>>> GetWorkoutsAsync()
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerWithClientsByIdAsync(currentUserAccessor.GetUserId());
            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<List<Workout>> { Data = [], Message = "No trainers with that id found", Success = false });
            }

            var clientList = await unitOfWork.TrainerRepository.GetTrainerClientsWithWorkoutsAsync(trainer);
            
            var workouts = unitOfWork.WorkoutRepository.GetSpecificClientsWorkoutsAsync(clientList);

            if (!workouts.Any())
            {
                return Ok(new ApiResponseDto<List<Workout>> { Data = [], Message = "No workout's found", Success = true });
            }

            return Ok(new ApiResponseDto<List<Workout>> { Data = workouts, Message = " workouts returned", Success = true });
        }

        /// <summary>
        /// Workout request for adding a workout for a specific client, utilised within SessionSyncService
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpPost("Auto/NewWorkout")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddNewAutoClientWorkoutAsync(string clientName, string workoutTitle, DateOnly workoutDate, int exerciseCount, int duration)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerByIdAsync(currentUserAccessor.GetUserId());
            if (trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = "Trainer not found", Success = false });
            }

            var client = await unitOfWork.ClientRepository.GetClientByNameWithTrainerAsync(trainer, clientName);
            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Client: {clientName} not found", Success = false });
            }

            unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
            await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workoutTitle, workoutDate, exerciseCount, duration);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Adding client unsuccessful", Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = clientName, Message = $"Workout added for client: {clientName}", Success = true });

        }

        /// <summary>
        /// Workout request for adding a workout for a specific client, utilised within SessionSyncService
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpPost("Manual/NewWorkout")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddNewManualClientWorkoutAsync([FromBody] WorkoutAddDto newWorkout)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdWithTrainerAsync(newWorkout.ClientId);
            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Client: {newWorkout.ClientName} not found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to add a workout for this client", Success = false });
            }

            var workoutOnTheDay = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateByIdAsync(client.Id, DateOnly.Parse(newWorkout.SessionDate));

            if (workoutOnTheDay != null) return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Workout for client {client.FirstName} already exists today", Success = false });

            unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
            await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, newWorkout.WorkoutTitle, DateOnly.Parse(newWorkout.SessionDate), newWorkout.ExerciseCount, newWorkout.Duration);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Adding workout for client: {client.FirstName} was unsuccessful", Success = false });
            }

            if (client.CurrentBlockSession == client.TotalBlockSessions)
            {
                await clientBlockTerminator.CreateAllAdequateEntityReminderAsync(client);
            }

            return Ok(new ApiResponseDto<string> { Data = newWorkout.ClientName, Message = $"Workout added for {newWorkout.ClientName} on {DateOnly.Parse(newWorkout.SessionDate)}", Success = true });

        }

        [Authorize(Roles = "Trainer")]
        [HttpPost("quickAddWorkout")]
        public async Task<ActionResult<ApiResponseDto<string?>>> QuickAddClientWorkoutAsync([FromBody] Client quickAddClient)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdWithTrainerAsync(quickAddClient.Id);

            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Client: {quickAddClient.FirstName} not found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to add a workout for this client", Success = false });
            }

            if(client.Trainer is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"{quickAddClient.FirstName}'s  associated trainer not found", Success = false });

            }

            var workoutOnTheDay = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateByIdAsync(client.Id, DateOnly.FromDateTime(DateTime.UtcNow));

            if (workoutOnTheDay != null) return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Workout for client {client.FirstName} already exists today", Success = false });

            unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
            await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, $" **{client.FirstName}'s Quick Added Workout **", DateOnly.FromDateTime(DateTime.UtcNow), 8, 60);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Quick adding workout for client: {client.FirstName} was unsuccessful", Success = false });
            }

            if (client.CurrentBlockSession == client.TotalBlockSessions)
            {
                await clientBlockTerminator.CreateAllAdequateEntityReminderAsync(client);
            }

            await notificationService.SendQuickAddTrainerReminderAsync(client.Trainer!, quickAddClient, DateTime.UtcNow);

            return Ok(new ApiResponseDto<string> { Data = null, Message = $"Quick add successful for {client.FirstName}", Success = true });
        }

        /// <summary>
        /// Workout request for updating an existing workout for a specific client
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpPut("updateWorkout")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateWorkoutDetailsAsync([FromBody] WorkoutUpdateDto newWorkoutInfo)
        {
            var workout = await unitOfWork.WorkoutRepository.GetWorkoutByIdWithClientAsync(newWorkoutInfo.Id);

            if (workout is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Workout not found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, workout, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to update this workout", Success = false });
            }

            unitOfWork.WorkoutRepository.UpdateWorkout(workout, newWorkoutInfo.WorkoutTitle, DateOnly.Parse(newWorkoutInfo.SessionDate), newWorkoutInfo.ExerciseCount, newWorkoutInfo.Duration);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Updating workout unsuccessful", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = workout.ClientName, Message = $"Workout with title: {workout.WorkoutTitle} at {workout.SessionDate} successfully updated", Success = true });


        }

        /// <summary>
        /// Workout request for removing a specific workout via client name & date
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpDelete("DeleteWorkout")]
        public async Task<ActionResult<ApiResponseDto<string>>> DeleteWorkoutAsync([FromQuery] int workoutId)
        {
            var workout = await unitOfWork.WorkoutRepository.GetWorkoutByIdWithClientAsync(workoutId);

            if (workout is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Workout doesn't exist", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, workout, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to remove this workout", Success = false });
            }

            if (workout.Client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"client doesn't exist", Success = false });
            }

            unitOfWork.WorkoutRepository.RemoveWorkout(workout);
            unitOfWork.ClientRepository.UpdateDeletingClientCurrentSession(workout.Client);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Removing workout was unsuccessful", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = workout.WorkoutTitle, Message = $"Workout titled: {workout.WorkoutTitle} at {workout.SessionDate} has been removed", Success = true });
        }
    }
}
