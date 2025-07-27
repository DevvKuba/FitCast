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
        [HttpGet("{date}/GetAllClientSessions")]
        public async Task<List<WorkoutDataDto>> GetAllDailyClientWorkouts(string date)
        {
            var clientSessions = await unitOfWork.WorkoutRepository.GetClientWorkoutsByDateAsync(DateOnly.Parse(date));
            var clientMappedSessions = new List<WorkoutDataDto>();

            if (clientSessions == null) throw new Exception($"No client sessions found on specificed date: {date}");

            foreach (var clientSession in clientSessions)
            {
                var clientDataDto = mapper.Map<WorkoutDataDto>(clientSession);
                clientMappedSessions.Add(clientDataDto);

            }
            return clientMappedSessions;

        }

        /// <summary>
        /// Workout request for the retrieval of a specific client's last workout
        /// </summary>
        [HttpGet("{clientName}/lastDate")]
        public async Task<Workout> GetLatestClientWorkout([FromHeader] string clientName)
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

            await unitOfWork.ClientRepository.RemoveWorkout(clientWorkout);

            if (await unitOfWork.Complete()) return Ok($"{clientName}'s workout at {sessionDate} has been removed");
            return BadRequest("Removing client unsuccessful");

            // should update the clients current session after added -1
        }

        /// <summary>
        /// Workout request for adding a workout for a specific client
        /// </summary>
        [HttpPost("{clientName}")]
        public async Task<ActionResult> AddNewClientWorkout(Workout workout, string clientName)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            if (client == null) return NotFound($"Client: {clientName} not found");
            client.Workouts.Add(workout);

            if (await unitOfWork.Complete()) return Ok($"Workout added for client: {clientName}");
            return BadRequest("Adding client unsuccessful");

            // should update the clients current session after added +1

        }


    }
}
