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
        public async Task<List<WorkoutDataDto>> GetAllDailyClientWorkouts()
        {
            var tadaysDateString = DateTime.Now.Date.ToString();
            var clientSessions = await unitOfWork.WorkoutRepository.GetClientWorkoutsAtDateAsync(DateOnly.Parse(tadaysDateString));
            var clientMappedSessions = new List<WorkoutDataDto>();

            if (clientSessions == null) throw new Exception($"No client sessions found on specificed date: {tadaysDateString}");

            foreach (var clientSession in clientSessions)
            {
                var clientDataDto = mapper.Map<WorkoutDataDto>(clientSession);
                clientMappedSessions.Add(clientDataDto);

            }
            return clientMappedSessions;

        }

        // does this client have a workout on x date ? true : false
        [HttpGet("{date}/GetWorkoutAtDate")]


        // give me a list of this clients workouts from x date []
        [HttpGet("{date}/GetWorkoutsFromDate")]


        /// <summary>
        /// Workout request for the retrieval of a specific client's last workout
        /// </summary>
        [HttpGet("{clientName}/lastDate")]
        public async Task<Workout> GetLatestClientWorkout(string clientName)
        {
            var latestWorkoutInfo = await unitOfWork.WorkoutRepository.GetLatestClientWorkoutAsync(clientName);

            if (latestWorkoutInfo == null) throw new Exception($"{clientName} has no workouts recorded");

            return latestWorkoutInfo;

        }

        /// <summary>
        /// Workout request for removing a specific workout via client name & date
        /// </summary>
        [HttpDelete("{clientName}/{sessionDate}")]
        public async Task<ActionResult> DeleteClientWorkout(string clientName, DateOnly sessionDate)
        {
            var clientWorkout = await unitOfWork.WorkoutRepository.GetSpecificClientWorkoutAsync(sessionDate, clientName);

            if (clientWorkout == null) return BadRequest($"Cannot find specified client: {clientName}'s workout at {sessionDate}");

            await unitOfWork.WorkoutRepository.RemoveWorkoutAsync(clientWorkout);
            await unitOfWork.ClientRepository.UpdateDeletingClientCurrentSessionAsync(clientName);

            if (await unitOfWork.Complete()) return Ok($"{clientName}'s workout at {sessionDate} has been removed");
            return BadRequest("Removing client unsuccessful");

        }

        /// <summary>
        /// Workout request for adding a workout for a specific client
        /// </summary>
        [HttpPost("/newWorkout")]
        public async Task<ActionResult> AddNewClientWorkout([FromBody] Workout workout)
        {
            // may need to look into specific api response a bit more, use fields to actually create that workout object to add
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(workout.ClientName);

            if (client == null) return NotFound($"Client: {workout.ClientName} not found");
            await unitOfWork.WorkoutRepository.AddWorkoutAsync(workout);
            await unitOfWork.ClientRepository.UpdateAddingClientCurrentSessionAsync(client.Name);

            if (await unitOfWork.Complete()) return Ok($"Workout added for client: {client.Name}");
            return BadRequest("Adding client unsuccessful");

        }

    }
}
