using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class WorkoutController(IUnitOfWork unitOfWork, IMapper mapper) : BaseAPIController
    {
        /// <summary>
        /// Workout request for the retrieval of all daily client sessions
        /// </summary>
        [HttpGet("{date}/GetAllDailySessions")]
        public async Task<ActionResult<List<Workout>>> GetAllDailyClientWorkoutsAsync()
        {
            var todaysDateString = DateTime.Now.Date.ToString();

            var clientSessions = await unitOfWork.WorkoutRepository.GetClientWorkoutsAtDateAsync(DateOnly.Parse(todaysDateString));
            var clientMappedSessions = new List<WorkoutDto>();

            if (clientSessions == null) return NotFound($"No client sessions found on specificed date: {todaysDateString}");

            foreach (var clientSession in clientSessions)
            {
                var clientDataDto = mapper.Map<WorkoutDto>(clientSession);
                clientMappedSessions.Add(clientDataDto);

            }
            return Ok(clientMappedSessions);

        }

        /// <summary>
        /// Workout request for retrieving a specific client workout, at a given date
        /// </summary>
        [HttpGet("{date}/GetWorkoutAtDate")]
        public async Task<ActionResult<Workout>> GetClientWorkoutAtDate(string clientName, DateOnly workoutDate)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateAsync(clientName, workoutDate);

            if (clientWorkout == null) return NotFound($"Client: {clientName}'s workout at {workoutDate} was not found.");

            return Ok(clientWorkout);
        }


        /// <summary>
        /// Workout request for retrieving a list of client workouts,
        /// from a given date
        /// </summary>
        [HttpGet("{date}/GetWorkoutsFromDate")]
        public async Task<ActionResult<List<Workout>>> GetClientWorkoutsFromDate(DateOnly workoutDate)
        {
            var clientWorkouts = await unitOfWork.WorkoutRepository.GetClientWorkoutsFromDateAsync(workoutDate);

            if (clientWorkouts == null) return NotFound($"No client sessions found on specificed date: {workoutDate}");

            return Ok(clientWorkouts);
        }

        /// <summary>
        /// Workout request for the retrieval of a specific client's last workout
        /// </summary>
        [HttpGet("{clientName}/lastDate")]
        public async Task<ActionResult<Workout>> GetLatestClientWorkout(string clientName)
        {
            var latestWorkoutInfo = await unitOfWork.WorkoutRepository.GetLatestClientWorkoutAsync(clientName);

            if (latestWorkoutInfo == null) return NotFound($"{clientName} has no workouts recorded");

            return Ok(latestWorkoutInfo);

        }

        /// <summary>
        /// Workout request for adding a workout for a specific client
        /// </summary>
        [HttpPost("/newWorkout")]
        public async Task<IActionResult> AddNewClientWorkout(string clientName, string workoutTitle, DateOnly workoutDate, int exerciseCount)
        {
            // may need to look into specific api response a bit more, use fields to actually create that workout object to add
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            if (client == null) return NotFound($"Client: {clientName} not found");

            unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client);
            await unitOfWork.WorkoutRepository.AddWorkoutAsync(client, workoutTitle, workoutDate, exerciseCount);

            if (await unitOfWork.Complete()) return Ok($"Workout added for client: {clientName}");
            return BadRequest("Adding client unsuccessful");

        }

        /// <summary>
        /// Workout request for removing a specific workout via client name & date
        /// </summary>
        [HttpDelete("{clientName}/{sessionDate}")]
        public async Task<IActionResult> DeleteClientWorkout(string clientName, DateOnly workoutDate)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateAsync(clientName, workoutDate);

            if (clientWorkout == null) return BadRequest($"Cannot find specified client: {clientName}'s workout at {workoutDate}");

            unitOfWork.ClientRepository.UpdateDeletingClientCurrentSessionAsync(clientWorkout.Client);
            unitOfWork.WorkoutRepository.RemoveWorkout(clientWorkout);

            if (await unitOfWork.Complete()) return Ok($"{clientName}'s workout at {workoutDate} has been removed");
            return BadRequest("Removing client unsuccessful");

        }

    }
}
