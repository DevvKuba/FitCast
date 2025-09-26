using AutoMapper;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class WorkoutController(IUnitOfWork unitOfWork, IMapper mapper) : BaseAPIController
    {
        /// <summary>
        /// Workout request for the retrieval of paginated workouts
        /// </summary>
        [HttpGet("/GetPaginatedWorkouts")]
        public async Task<ActionResult<ApiResponseDto<List<Workout>>>> GetPaginatedWorkouts([FromQuery] int first, [FromQuery] int rows)
        {
            var paginatedWorkouts = await unitOfWork.WorkoutRepository.GetPaginatedWorkoutsAsync(first, rows);

            if (!paginatedWorkouts.Any()) return NotFound(new ApiResponseDto<List<Workout>> { Data = [], Message = "No workout's found", Success = false });

            return Ok(new ApiResponseDto<List<Workout>> { Data = paginatedWorkouts, Message = "Paginated workouts returned", Success = true });
        }

        /// <summary>
        /// Workout request for the retrieval of all daily workouts
        /// </summary>
        [HttpGet("/GetAllDailySessions")]
        public async Task<ActionResult<ApiResponseDto<List<Workout>>>> GetAllDailyClientWorkoutsAsync()
        {
            var todaysDateString = DateTime.Now.Date.ToString();
            var todaysDate = DateOnly.Parse(todaysDateString[0..10]);

            var clientSessions = await unitOfWork.WorkoutRepository.GetClientWorkoutsAtDateAsync(todaysDate);

            if (clientSessions == null || !clientSessions.Any())
            {
                return NotFound(new ApiResponseDto<List<Workout>> { Data = [], Message = $"No client sessions found on specified date: {todaysDateString}", Success = false });
            }

            return Ok(new ApiResponseDto<List<Workout>> { Data = clientSessions, Message = "Daily workouts retrieved successfully", Success = true });
        }

        /// <summary>
        /// Workout request for retrieving a specific client workout, at a given date
        /// </summary>
        [HttpGet("{clientName}/{workoutDate}/GetWorkoutAtDate")]
        public async Task<ActionResult<ApiResponseDto<Workout>>> GetClientWorkoutAtDateAsync(string clientName, DateOnly workoutDate)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateAsync(clientName, workoutDate);

            if (clientWorkout == null)
            {
                return NotFound(new ApiResponseDto<Workout> { Data = null, Message = $"Client: {clientName}'s workout at {workoutDate} was not found.", Success = false });
            }

            return Ok(new ApiResponseDto<Workout> { Data = clientWorkout, Message = "Workout retrieved successfully", Success = true });
        }

        /// <summary>
        /// Workout request for retrieving a list of client workouts,
        /// from a given date
        /// </summary>
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
        [HttpGet("{clientName}/lastDate")]
        public async Task<ActionResult<ApiResponseDto<Workout>>> GetLatestClientWorkoutAsync(string clientName)
        {
            var latestWorkoutInfo = await unitOfWork.WorkoutRepository.GetLatestClientWorkoutAsync(clientName);

            if (latestWorkoutInfo == null)
            {
                return NotFound(new ApiResponseDto<Workout> { Data = null, Message = $"{clientName} has no workouts recorded", Success = false });
            }

            return Ok(new ApiResponseDto<Workout> { Data = latestWorkoutInfo, Message = $"Latest workout for {clientName} retrieved successfully", Success = true });
        }

        /// <summary>
        /// Workout request for adding a workout for a specific client
        /// </summary>
        [HttpPost("/newWorkout")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddNewClientWorkoutAsync(string clientName, string workoutTitle, DateOnly workoutDate, int exerciseCount)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            if (client == null) return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Client: {clientName} not found", Success = false });

            unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
            await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workoutTitle, workoutDate, exerciseCount);

            if (await unitOfWork.Complete()) return Ok(new ApiResponseDto<string> { Data = clientName, Message = $"Workout added for client: {clientName}", Success = true });

            return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Adding client unsuccessful", Success = false });
        }

        /// <summary>
        /// Workout request for removing a specific workout via client name & date
        /// </summary>
        [HttpDelete("{clientName}/{sessionDate}")]
        public async Task<ActionResult<ApiResponseDto<string>>> DeleteClientWorkoutAsync(string clientName, DateOnly workoutDate)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateAsync(clientName, workoutDate);

            if (clientWorkout == null) return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Cannot find specified client: {clientName}'s workout at {workoutDate}", Success = false });

            unitOfWork.ClientRepository.UpdateDeletingClientCurrentSession(clientWorkout.Client);
            unitOfWork.WorkoutRepository.RemoveWorkout(clientWorkout);

            if (await unitOfWork.Complete()) return Ok(new ApiResponseDto<string> { Data = clientName, Message = $"{clientName}'s workout at {workoutDate} has been removed", Success = true });

            return BadRequest(new ApiResponseDto<string> { Data = null, Message = "Removing client unsuccessful", Success = false });
        }
    }
}
