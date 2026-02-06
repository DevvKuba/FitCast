using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    [Authorize]
    public class WorkoutController(IUnitOfWork unitOfWork, IClientBlockTerminationHelper clientBlockTermination, IMapper mapper) : BaseAPIController
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
        public async Task<ActionResult<ApiResponseDto<List<Workout>>>> GetWorkouts([FromQuery] int trainerId)
        {
            var trainer = await unitOfWork.TrainerRepository.GetTrainerWithClientsByIdAsync(trainerId);
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
        /// Workout request for the retrieval of all daily workouts
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpGet("GetAllDailySessions")]
        public async Task<ActionResult<ApiResponseDto<List<Workout>>>> GetAllDailyClientWorkoutsAsync()
        {
            var todaysDateString = DateTime.Now.Date.ToString();
            var todaysDate = DateOnly.Parse(todaysDateString[0..10]);

            var clientSessions = await unitOfWork.WorkoutRepository.GetClientWorkoutsAtDateAsync(todaysDate);

            if (clientSessions is null || !clientSessions.Any())
            {
                return NotFound(new ApiResponseDto<List<Workout>> { Data = [], Message = $"No client sessions found on specified date: {todaysDateString}", Success = false });
            }

            return Ok(new ApiResponseDto<List<Workout>> { Data = clientSessions, Message = "Daily workouts retrieved successfully", Success = true });
        }

        /// <summary>
        /// Workout request for retrieving a specific client workout, at a given date
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpGet("{clientName}/{workoutDate}/GetWorkoutAtDate")]
        public async Task<ActionResult<ApiResponseDto<Workout>>> GetClientWorkoutAtDateAsync(string clientName, DateOnly workoutDate)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateByNameAsync(clientName, workoutDate);
            if (clientWorkout is null)
            {
                return NotFound(new ApiResponseDto<Workout> { Data = null, Message = $"Client: {clientName}'s workout at {workoutDate} was not found.", Success = false });
            }

            return Ok(new ApiResponseDto<Workout> { Data = clientWorkout, Message = "Workout retrieved successfully", Success = true });
        }

        /// <summary>
        /// Workout request for retrieving a list of client workouts,
        /// from a given date
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpGet("{workoutDate}/GetWorkoutsFromDate")]
        public async Task<ActionResult<ApiResponseDto<List<Workout>>>> GetClientWorkoutsFromDateAsync(DateOnly workoutDate)
        {
            var clientWorkouts = await unitOfWork.WorkoutRepository.GetClientWorkoutsFromDateAsync(workoutDate);
            if (!clientWorkouts.Any())
            {
                return NotFound(new ApiResponseDto<List<Workout>> { Data = [], Message = $"No client sessions found on specified date: {workoutDate}", Success = false });
            }

            return Ok(new ApiResponseDto<List<Workout>> { Data = clientWorkouts, Message = "Workouts from date retrieved successfully", Success = true });
        }

        /// <summary>
        /// Workout request for the retrieval of a specific client's last workout
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpGet("{clientName}/lastDate")]
        public async Task<ActionResult<ApiResponseDto<Workout>>> GetLatestClientWorkoutAsync(string clientName)
        {
            var latestWorkoutInfo = await unitOfWork.WorkoutRepository.GetLatestClientWorkoutAsync(clientName);
            if (latestWorkoutInfo is null)
            {
                return NotFound(new ApiResponseDto<Workout> { Data = null, Message = $"{clientName} has no workouts recorded", Success = false });
            }

            return Ok(new ApiResponseDto<Workout> { Data = latestWorkoutInfo, Message = $"Latest workout for {clientName} retrieved successfully", Success = true });
        }

        /// <summary>
        /// Workout request for adding a workout for a specific client, utilised within SessionSyncService
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpPost("Auto/NewWorkout")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddNewAutoClientWorkoutAsync(string clientName, string workoutTitle, DateOnly workoutDate, int exerciseCount, int duration)
        {
            // TODO may need to change to Id even for SessionSyncService
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);
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

            unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
            await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, newWorkout.WorkoutTitle, DateOnly.Parse(newWorkout.SessionDate), newWorkout.ExerciseCount, newWorkout.Duration);


            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Adding client unsuccessful", Success = false });
            }

            if (client.CurrentBlockSession == client.TotalBlockSessions)
            {
                if (client.Trainer is not null)
                {
                    await clientBlockTermination.CreateAdequateRemindersAndPaymentsAsync(client);
                }
            }

            return Ok(new ApiResponseDto<string> { Data = newWorkout.ClientName, Message = $"Workout added for client: {newWorkout.ClientName}", Success = true });

        }

        /// <summary>
        /// Workout request for updating an existing workout for a specific client
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpPut("updateWorkout")]
        public async Task<ActionResult<ApiResponseDto<string>>> UpdateWorkoutDetails([FromBody] WorkoutUpdateDto newWorkoutInfo)
        {
            var workout = await unitOfWork.WorkoutRepository.GetWorkoutByIdAsync(newWorkoutInfo.Id);

            if (workout is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Workout not found", Success = false });
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
        [HttpDelete("{clientName}/{sessionDate}")]
        public async Task<ActionResult<ApiResponseDto<string>>> DeleteClientWorkoutAsync(string clientName, DateOnly workoutDate)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateByNameAsync(clientName, workoutDate);
            if (clientWorkout is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Cannot find specified client: {clientName}'s workout at {workoutDate}", Success = false });
            }
            
            unitOfWork.ClientRepository.UpdateDeletingClientCurrentSession(clientWorkout.Client!);
            unitOfWork.WorkoutRepository.RemoveWorkout(clientWorkout);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Removing client was unsuccessful", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = clientName, Message = $"{clientName}'s workout at {workoutDate} has been removed", Success = true });

        }

        /// <summary>
        /// Workout request for removing a specific workout via client name & date
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpDelete("DeleteWorkout")]
        public async Task<ActionResult<ApiResponseDto<string>>> DeleteWorkoutAsync([FromQuery] int workoutId)
        {
            var workout = await unitOfWork.WorkoutRepository.GetWorkoutByIdAsync(workoutId);

            if (workout is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Workout doesn't exist", Success = false });
            }

            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(workout.ClientId);

            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"client doesn't exist", Success = false });
            }

            unitOfWork.WorkoutRepository.RemoveWorkout(workout);
            unitOfWork.ClientRepository.UpdateDeletingClientCurrentSession(client);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Removing workout was unsuccessful", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = workout.WorkoutTitle, Message = $"Workout titled: {workout.WorkoutTitle} at {workout.SessionDate} has been removed", Success = true });
        }
    }
}
