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
        public async Task<ActionResult<List<Workout>>> GetAllDailyClientWorkouts()
        {
            var todaysDateString = DateTime.Now.Date.ToString();

            var clientSessions = await unitOfWork.WorkoutRepository.GetClientWorkoutsAtDateAsync(DateOnly.Parse(todaysDateString));
            var clientMappedSessions = new List<WorkoutDataDto>();

            if (clientSessions == null) return NotFound($"No client sessions found on specificed date: {todaysDateString}");

            foreach (var clientSession in clientSessions)
            {
                var clientDataDto = mapper.Map<WorkoutDataDto>(clientSession);
                clientMappedSessions.Add(clientDataDto);

            }
            return Ok(clientMappedSessions);

        }

        /// <summary>
        /// Workout request for retrieving a specific client workout, at a given date
        /// </summary>
        [HttpGet("{date}/GetWorkoutAtDate")]
        public async Task<ActionResult<Workout>> GetClientWorkoutAtDate(string clientName, DateOnly date)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetClientWorkoutAtDateAsync(clientName, date);

            if (clientWorkout == null) return NotFound($"Client: {clientName}'s workout at {date} was not found.");

            return Ok(clientWorkout);
        }


        /// <summary>
        /// Workout request for retrieving a list of client workouts,
        /// from a given date
        /// </summary>
        [HttpGet("{date}/GetWorkoutsFromDate")]
        public async Task<ActionResult<List<Workout>>> GetClientWorkoutsFromDate(DateOnly date)
        {
            var clientWorkouts = await unitOfWork.WorkoutRepository.GetClientWorkoutsFromDateAsync(date);

            if (clientWorkouts == null) return NotFound($"No client sessions found on specificed date: {date}");

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
        public async Task<IActionResult> AddNewClientWorkout([FromBody] Workout workout)
        {
            // may need to look into specific api response a bit more, use fields to actually create that workout object to add
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(workout.ClientName);

            if (client == null) return NotFound($"Client: {workout.ClientName} not found");
            await unitOfWork.WorkoutRepository.AddWorkoutAsync(workout);
            await unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client.Name);

            if (await unitOfWork.Complete()) return Ok($"Workout added for client: {client.Name}");
            return BadRequest("Adding client unsuccessful");

        }

        /// <summary>
        /// Workout request for removing a specific workout via client name & date
        /// </summary>
        [HttpDelete("{clientName}/{sessionDate}")]
        public async Task<IActionResult> DeleteClientWorkout(string clientName, DateOnly sessionDate)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetSpecificClientWorkoutAsync(sessionDate, clientName);

            if (clientWorkout == null) return BadRequest($"Cannot find specified client: {clientName}'s workout at {sessionDate}");

            await unitOfWork.WorkoutRepository.RemoveWorkoutAsync(clientWorkout);
            await unitOfWork.ClientRepository.UpdateDeletingClientCurrentSessionAsync(clientName);

            if (await unitOfWork.Complete()) return Ok($"{clientName}'s workout at {sessionDate} has been removed");
            return BadRequest("Removing client unsuccessful");

        }

    }
}
